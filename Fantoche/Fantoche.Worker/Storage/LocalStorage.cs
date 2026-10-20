using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using DuckDB.NET.Data;
using Fantoche.Plugins.Shared;
using Fantoche.Shared.Data.Execution;

namespace Fantoche.Worker.Storage;

/// <summary>
/// The storage as a task of a run sees it, in a <see cref="StorageFolder"/> which may be shared
/// between machines : every table is a Parquet file of its own, files are kept as they are. Tables
/// are immutable, so a file never changes once written and several processes read the same folder
/// without stepping on each other. Nothing is created until a task uses it.
/// <para>
/// What a task saves goes to the folder of its run, and its handle names that run : a task reads the
/// tables and files of any run, the ones of the workflow it belongs to as well as the ones it was
/// handed as parameters. Those handles come from its parameters, so their ids are checked before
/// reaching a query or a path.
/// </para>
/// </summary>
public sealed class LocalStorage : IStorage
{
    private const string TablePrefix = "t_";
    private static readonly Regex TableIds = new($@"\b{TablePrefix}[0-9a-f]{{32}}_[0-9a-f]{{32}}_[0-9a-f]{{32}}\b", RegexOptions.Compiled);

    private readonly StorageFolder _folder;
    private readonly StorageOwner _owner;

    public LocalStorage(StorageFolder folder, StorageOwner owner)
    {
        _folder = folder;
        _owner = owner;
    }

    #region Tables

    public async Task<TableRef> CreateTableAsync(string query, IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellation = default)
    {
        string id = $"{TablePrefix}{HandleId(_owner, Guid.NewGuid())}";
        string path = PathOf(id);
        // Written under another name first, so a reader never finds a table half written.
        string partialPath = $"{path}.partial";
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        using DuckDBConnection connection = await OpenAsync(query, cancellation);
        using (DuckDBCommand command = connection.CreateCommand())
        {
            command.CommandText = $"CREATE TEMP TABLE {id} AS {query}";
            foreach (var parameter in parameters ?? new Dictionary<string, object?>())
                command.Parameters.Add(new DuckDBParameter(parameter.Key, parameter.Value));
            await command.ExecuteNonQueryAsync(cancellation);
        }

        using (DuckDBCommand command = connection.CreateCommand())
        {
            command.CommandText = $"COPY {id} TO {Literal(partialPath)} (FORMAT parquet)";
            await command.ExecuteNonQueryAsync(cancellation);
        }
        File.Move(partialPath, path);

        return new TableRef { Id = id, Columns = await ColumnsAsync(connection, path, cancellation) };
    }

    public async IAsyncEnumerable<IReadOnlyDictionary<string, object?>> ReadRowsAsync(TableRef table,
        [EnumeratorCancellation] CancellationToken cancellation = default)
    {
        string path = PathOf(Identifier(table));
        using DuckDBConnection connection = await OpenAsync("", cancellation);
        using DuckDBCommand command = connection.CreateCommand();
        command.CommandText = $"SELECT * FROM read_parquet({Literal(path)})";

        using var reader = await command.ExecuteReaderAsync(cancellation);
        while (await reader.ReadAsync(cancellation))
        {
            var row = new Dictionary<string, object?>(reader.FieldCount);
            for (int i = 0; i < reader.FieldCount; i++)
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            yield return row;
        }
    }

    /// <summary>
    /// The columns as they were stored, which is what reading the table gives back.
    /// </summary>
    private static async Task<List<TableColumn>> ColumnsAsync(DuckDBConnection connection, string path, CancellationToken cancellation)
    {
        using DuckDBCommand command = connection.CreateCommand();
        command.CommandText = $"DESCRIBE SELECT * FROM read_parquet({Literal(path)})";

        List<TableColumn> columns = [];
        using var reader = await command.ExecuteReaderAsync(cancellation);
        while (await reader.ReadAsync(cancellation))
            columns.Add(new TableColumn { Name = reader.GetString(0), Type = reader.GetString(1) });
        return columns;
    }

    /// <summary>
    /// An in-memory database holding the tables [query] names, loaded from their files. Loaded rather
    /// than read through a view : the plugins rely on the <c>rowid</c> of a table, in the order of its
    /// rows, which only a table has. One per operation : a connection is unusable by several threads
    /// at once.
    /// </summary>
    private async Task<DuckDBConnection> OpenAsync(string query, CancellationToken cancellation)
    {
        var connection = new DuckDBConnection("Data Source=:memory:");
        connection.Open();

        var ids = TableIds.Matches(query).Select(x => x.Value).Distinct();
        foreach (string id in ids)
        {
            // Left out when unknown : the query then fails on it, naming it.
            string path = PathOf(id);
            if (!File.Exists(path))
                continue;

            using DuckDBCommand command = connection.CreateCommand();
            command.CommandText = $"CREATE TEMP TABLE {id} AS SELECT * FROM read_parquet({Literal(path)})";
            await command.ExecuteNonQueryAsync(cancellation);
        }

        return connection;
    }

    /// <summary>
    /// Where the table [tableId] is stored, which only a table this storage named has.
    /// </summary>
    private string PathOf(string tableId)
    {
        if (!tableId.StartsWith(TablePrefix) || !TryParseHandleId(tableId[TablePrefix.Length..], out var owner, out Guid table))
            throw new ArgumentException($"[{tableId}] is not a table of the storage.");
        return Path.Combine(_folder.PathOf(owner), "tables", $"{table:N}.parquet");
    }

    private static string Literal(string path) => $"'{path.Replace("'", "''")}'";

    /// <summary>
    /// [table] as it is written in a query, only once checked to be a table this storage named.
    /// </summary>
    private static string Identifier(TableRef table)
    {
        if (!table.Id.StartsWith(TablePrefix) || !TryParseHandleId(table.Id[TablePrefix.Length..], out _, out _))
            throw new ArgumentException($"[{table.Id}] is not a table of the storage.", nameof(table));
        return table.Id;
    }

    #endregion

    #region Files

    public async Task<FileRef> SaveFileAsync(string name, Stream content, CancellationToken cancellation = default)
    {
        var file = new FileRef { Id = HandleId(_owner, Guid.NewGuid()), Name = name };
        string path = PathOf(file);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using FileStream stream = File.Create(path);
        await content.CopyToAsync(stream, cancellation);
        return file;
    }

    public Task<Stream> OpenFileAsync(FileRef file, CancellationToken cancellation = default)
        => Task.FromResult<Stream>(File.OpenRead(PathOf(file)));

    /// <summary>
    /// Where [file] is stored, only once checked to stay in the folder of the storage.
    /// </summary>
    private string PathOf(FileRef file)
    {
        if (!TryParseHandleId(file.Id, out var owner, out Guid id))
            throw new ArgumentException($"[{file.Id}] is not a file of the storage.", nameof(file));
        if (string.IsNullOrWhiteSpace(file.Name) || file.Name != Path.GetFileName(file.Name) || file.Name is "." or "..")
            throw new ArgumentException($"[{file.Name}] is not a valid file name.", nameof(file));
        return Path.Combine(_folder.PathOf(owner), "files", $"{id:N}", file.Name);
    }

    #endregion

    /// <summary>
    /// The id of a handle : the run holding the item, then the item, as "{task}_{instance}_{item}".
    /// </summary>
    private static string HandleId(StorageOwner owner, Guid item) => $"{owner.TaskId:N}_{owner.InstanceId:N}_{item:N}";

    private static bool TryParseHandleId(string id, out StorageOwner owner, out Guid item)
    {
        owner = new StorageOwner(Guid.Empty, Guid.Empty);
        item = Guid.Empty;

        string[] parts = id.Split('_');
        if (parts.Length != 3)
            return false;
        if (!Guid.TryParseExact(parts[0], "N", out Guid taskId) || !Guid.TryParseExact(parts[1], "N", out Guid instanceId))
            return false;
        if (!Guid.TryParseExact(parts[2], "N", out item))
            return false;

        owner = new StorageOwner(taskId, instanceId);
        return true;
    }
}
