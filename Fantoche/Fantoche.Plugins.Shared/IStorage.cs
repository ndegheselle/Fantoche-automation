using NJsonSchema.Annotations;

namespace Fantoche.Plugins.Shared;

/// <summary>
/// Types the engine knows : their schema is tagged with <see cref="SchemaKey"/> rather than read as
/// any other object, so an editor can handle them on their own (e.g. a table picker).
/// </summary>
public static class KnownTypes
{
    public const string SchemaKey = "x-fantoche-type";
    public const string Table = "table";
    public const string File = "file";
}

public class TableColumn
{
    public string Name { get; set; } = "";

    /// <summary>
    /// SQL type of the column (e.g. "VARCHAR", "BIGINT").
    /// </summary>
    public string Type { get; set; } = "";
}

/// <summary>
/// Handle on a table of the <see cref="IStorage"/> : what tasks hand over to each other instead of
/// the rows themselves.
/// </summary>
[JsonSchemaExtensionData(KnownTypes.SchemaKey, KnownTypes.Table)]
public class TableRef
{
    /// <summary>
    /// Unique, and the name of the table in the queries of <see cref="IStorage.CreateTableAsync"/>.
    /// </summary>
    public string Id { get; set; } = "";

    public List<TableColumn> Columns { get; set; } = [];
}

/// <summary>
/// Handle on a file of the <see cref="IStorage"/>.
/// </summary>
[JsonSchemaExtensionData(KnownTypes.SchemaKey, KnownTypes.File)]
public class FileRef
{
    public string Id { get; set; } = "";

    /// <summary>
    /// Name the file was saved with, extension included.
    /// </summary>
    public string Name { get; set; } = "";
}

/// <summary>
/// Files and tables a task saves for the ones after it. Tables are immutable : a task transforming
/// one creates a new one from a query on it.
/// </summary>
public interface IStorage
{
    /// <summary>
    /// Create a table from a CSV file, its columns and their types being detected from the content.
    /// </summary>
    Task<TableRef> ImportCsvAsync(string path, CancellationToken cancellation = default);

    /// <summary>
    /// Create a table from the result of [query], a DuckDB SQL select reading other tables by their
    /// <see cref="TableRef.Id"/>, e.g. <c>$"SELECT * FROM {source.Id} WHERE amount > $min"</c>.
    /// </summary>
    Task<TableRef> CreateTableAsync(string query, IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellation = default);

    /// <summary>
    /// Rows of [table] in order, by column name.
    /// </summary>
    IAsyncEnumerable<IReadOnlyDictionary<string, object?>> ReadRowsAsync(TableRef table,
        CancellationToken cancellation = default);

    Task<FileRef> SaveFileAsync(string name, Stream content, CancellationToken cancellation = default);

    Task<Stream> OpenFileAsync(FileRef file, CancellationToken cancellation = default);
}
