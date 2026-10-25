using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using DuckDB.NET.Data;
using Fantoche.Plugins.Shared;
using Newtonsoft.Json.Linq;

namespace Fantoche.Worker.Storage;

/// <summary>
/// Storage in a folder, the one of an execution (see <see cref="ExecutionStorages"/>) : every table
/// is a Parquet file of its own, files are kept as they are. Tables are immutable, so a file never
/// changes once written and the branches of a workflow read it without stepping on each other.
/// Nothing is created until a task uses it.
/// <para>
/// The handles a task is given come from its parameters, so their ids are checked before reaching
/// a query or a path.
/// </para>
/// </summary>
public sealed class LocalStorage : IStorage
{
    private const string TablePrefix = "t_";
    private const string FilePrefix = "f_";
    private static readonly Regex TableIds = new($@"\b{TablePrefix}[0-9a-f]{{32}}\b", RegexOptions.Compiled);

    private readonly string _folder;
    private readonly string _tablesFolder;
    private readonly string _filesFolder;

    public LocalStorage(string folder)
    {
        _folder = folder;
        _tablesFolder = Path.Combine(folder, "tables");
        _filesFolder = Path.Combine(folder, "files");
    }

    #region Tables

    public async Task<TableRef> CreateTableAsync(string query, IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellation = default)
    {
        List<TableRef> tables = await CreateTablesAsync([query], parameters, cancellation);
        return tables[0];
    }

    public async Task<List<TableRef>> CreateTablesAsync(IReadOnlyList<string> queries, IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellation = default)
    {
        Directory.CreateDirectory(_tablesFolder);
        using DuckDBConnection connection = await OpenAsync(string.Join('\n', queries), cancellation);

        List<TableRef> tables = [];
        foreach (string query in queries)
            tables.Add(await CreateTableAsync(connection, query, parameters, cancellation));
        return tables;
    }

    private async Task<TableRef> CreateTableAsync(DuckDBConnection connection, string query,
        IReadOnlyDictionary<string, object?>? parameters, CancellationToken cancellation)
    {
        string id = $"{TablePrefix}{Guid.NewGuid():N}";
        string path = PathOf(id);
        // Written under another name first, so a reader never finds a table half written.
        string partialPath = $"{path}.partial";

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

        // Dropped once written : the next queries of the connection don't need it, the memory does.
        using (DuckDBCommand command = connection.CreateCommand())
        {
            command.CommandText = $"DROP TABLE {id}";
            await command.ExecuteNonQueryAsync(cancellation);
        }
        File.Move(partialPath, path);

        return new TableRef { Id = id, Columns = await ColumnsAsync(connection, path, cancellation) };
    }

    public async Task<long> CountRowsAsync(TableRef table, CancellationToken cancellation = default)
    {
        string path = PathOf(Identifier(table));
        using DuckDBConnection connection = await OpenAsync("", cancellation);
        return await CountAsync(connection, path, cancellation);
    }

    private static async Task<long> CountAsync(DuckDBConnection connection, string path, CancellationToken cancellation)
    {
        using DuckDBCommand command = connection.CreateCommand();
        command.CommandText = $"SELECT count(*) FROM read_parquet({Literal(path)})";
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellation));
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
            yield return RowOf(reader);
    }

    public async Task<TablePage> ReadPageAsync(TableRef table, long offset, int limit, CancellationToken cancellation = default)
    {
        string path = PathOf(Identifier(table));
        if (!File.Exists(path))
            throw new FileNotFoundException($"The table [{table.Id}] isn't in this storage.", path);

        using DuckDBConnection connection = await OpenAsync("", cancellation);
        List<TableColumn> columns = await ColumnsAsync(connection, path, cancellation);

        long total = await CountAsync(connection, path, cancellation);

        List<IReadOnlyDictionary<string, object?>> rows = [];
        using (DuckDBCommand command = connection.CreateCommand())
        {
            command.CommandText = $"SELECT * FROM read_parquet({Literal(path)}) LIMIT {limit} OFFSET {offset}";
            using var reader = await command.ExecuteReaderAsync(cancellation);
            while (await reader.ReadAsync(cancellation))
                rows.Add(RowOf(reader));
        }

        return new TablePage(columns, total, rows);
    }

    private static Dictionary<string, object?> RowOf(DbDataReader reader)
    {
        var row = new Dictionary<string, object?>(reader.FieldCount);
        for (int i = 0; i < reader.FieldCount; i++)
            row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
        return row;
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

    private string PathOf(string tableId) => Path.Combine(_tablesFolder, $"{tableId}.parquet");

    private static string Literal(string path) => $"'{path.Replace("'", "''")}'";

    /// <summary>
    /// [table] as it is written in a query, only once checked to be a table this storage named.
    /// </summary>
    private static string Identifier(TableRef table)
    {
        if (!table.Id.StartsWith(TablePrefix) || !Guid.TryParseExact(table.Id[TablePrefix.Length..], "N", out _))
            throw new ArgumentException($"[{table.Id}] is not a table of the storage.", nameof(table));
        return table.Id;
    }

    #endregion

    #region Files

    public async Task<FileRef> SaveFileAsync(string name, Stream content, CancellationToken cancellation = default)
    {
        var file = new FileRef { Id = $"{FilePrefix}{Guid.NewGuid():N}", Name = name };
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
        if (!file.Id.StartsWith(FilePrefix) || !Guid.TryParseExact(file.Id[FilePrefix.Length..], "N", out _))
            throw new ArgumentException($"[{file.Id}] is not a file of the storage.", nameof(file));
        if (string.IsNullOrWhiteSpace(file.Name) || file.Name != Path.GetFileName(file.Name) || file.Name is "." or "..")
            throw new ArgumentException($"[{file.Name}] is not a valid file name.", nameof(file));
        return Path.Combine(_filesFolder, file.Id, file.Name);
    }

    #endregion
}
