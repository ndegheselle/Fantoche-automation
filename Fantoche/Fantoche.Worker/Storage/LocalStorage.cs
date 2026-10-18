using System.Runtime.CompilerServices;
using DuckDB.NET.Data;
using Fantoche.Plugins.Shared;

namespace Fantoche.Worker.Storage;

/// <summary>
/// Storage on the local disk : tables in a DuckDB database, files in a folder, both under the
/// given folder. Nothing is created until a task uses it.
/// <para>
/// The handles a task is given come from its parameters, so their ids are checked before reaching
/// a query or a path.
/// </para>
/// </summary>
public sealed class LocalStorage : IStorage, IDisposable
{
    private const string TablePrefix = "t_";

    private readonly string _filesFolder;

    private readonly string _connectionString;

    /// <summary>
    /// Opened on first use and held until disposal, see <see cref="KeepDatabaseLoaded"/>.
    /// </summary>
    private readonly Lazy<DuckDBConnection> _keepAliveConnection;

    public LocalStorage(string folder)
    {
        _filesFolder = Path.Combine(folder, "files");
        _connectionString = $"Data Source={Path.Combine(folder, "tables.duckdb")}";
        _keepAliveConnection = new(() =>
        {
            Directory.CreateDirectory(folder);
            var connection = new DuckDBConnection(_connectionString);
            connection.Open();
            return connection;
        });
    }

    public void Dispose()
    {
        if (_keepAliveConnection.IsValueCreated)
            _keepAliveConnection.Value.Dispose();
    }

    #region Tables

    public async Task<TableRef> CreateTableAsync(string query, IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellation = default)
    {
        string id = $"{TablePrefix}{Guid.NewGuid():N}";
        using DuckDBConnection connection = Open();

        using (DuckDBCommand command = connection.CreateCommand())
        {
            command.CommandText = $"CREATE TABLE {id} AS {query}";
            foreach (var parameter in parameters ?? new Dictionary<string, object?>())
                command.Parameters.Add(new DuckDBParameter(parameter.Key, parameter.Value));
            await command.ExecuteNonQueryAsync(cancellation);
        }

        return new TableRef { Id = id, Columns = await ColumnsAsync(connection, id, cancellation) };
    }

    public async IAsyncEnumerable<IReadOnlyDictionary<string, object?>> ReadRowsAsync(TableRef table,
        [EnumeratorCancellation] CancellationToken cancellation = default)
    {
        string id = Identifier(table);
        using DuckDBConnection connection = Open();
        using DuckDBCommand command = connection.CreateCommand();
        command.CommandText = $"SELECT * FROM {id} ORDER BY rowid";

        using var reader = await command.ExecuteReaderAsync(cancellation);
        while (await reader.ReadAsync(cancellation))
        {
            var row = new Dictionary<string, object?>(reader.FieldCount);
            for (int i = 0; i < reader.FieldCount; i++)
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            yield return row;
        }
    }

    private static async Task<List<TableColumn>> ColumnsAsync(DuckDBConnection connection, string id, CancellationToken cancellation)
    {
        using DuckDBCommand command = connection.CreateCommand();
        command.CommandText = "SELECT column_name, data_type FROM information_schema.columns WHERE table_name = $table ORDER BY ordinal_position";
        command.Parameters.Add(new DuckDBParameter("table", id));

        List<TableColumn> columns = [];
        using var reader = await command.ExecuteReaderAsync(cancellation);
        while (await reader.ReadAsync(cancellation))
            columns.Add(new TableColumn { Name = reader.GetString(0), Type = reader.GetString(1) });
        return columns;
    }

    /// <summary>
    /// Every operation opens a connection of its own, a connection being unusable by several threads
    /// at once.
    /// </summary>
    private DuckDBConnection Open()
    {
        KeepDatabaseLoaded();
        var connection = new DuckDBConnection(_connectionString);
        connection.Open();
        return connection;
    }

    /// <summary>
    /// DuckDB unloads a database once its last connection closes : holding one open spares reloading
    /// it on every operation. The first call also creates the folder of the storage.
    /// </summary>
    private void KeepDatabaseLoaded() => _ = _keepAliveConnection.Value;

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
        var file = new FileRef { Id = Guid.NewGuid().ToString("N"), Name = name };
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
        if (!Guid.TryParseExact(file.Id, "N", out _))
            throw new ArgumentException($"[{file.Id}] is not a file of the storage.", nameof(file));
        if (string.IsNullOrWhiteSpace(file.Name) || file.Name != Path.GetFileName(file.Name) || file.Name is "." or "..")
            throw new ArgumentException($"[{file.Name}] is not a valid file name.", nameof(file));
        return Path.Combine(_filesFolder, file.Id, file.Name);
    }

    #endregion
}
