using NJsonSchema.Annotations;

namespace Fantoche.Plugins.Shared;

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

    /// <summary>
    /// What the workflow calls the table, the file it was imported from for instance : what tells
    /// its columns apart from the ones of another table, once merged with it. Handed over by the
    /// tasks creating a table from it.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// The columns as they are stored.
    /// </summary>
    public List<TableColumn> Columns { get; set; } = [];

    /// <summary>
    /// The names the workflow reads the columns by, each one the stored column it stands for : the
    /// clear names given to the columns of a file without copying the table. A mapping is
    /// referenced as any other value of the context, "$previous.Mapping.Name" being the stored
    /// column, so an editor offers the mapped columns of a table where a column is expected.
    /// <para>
    /// A task creating a table from a mapped one stores the mapped columns under their names, and
    /// hands the mapping over (each name then standing for itself) : what is written against the
    /// names keeps reading the same columns down the workflow.
    /// </para>
    /// </summary>
    public Dictionary<string, string> Mapping { get; set; } = [];

    /// <summary>
    /// The stored column [name] stands for : the one it is mapped to, or the column of that name.
    /// </summary>
    public string ColumnOf(string name) => Mapping.GetValueOrDefault(name, name);

    /// <summary>
    /// The name the workflow reads the stored [column] by : the first one mapped to it, or its own.
    /// </summary>
    public string NameOf(string column)
        => Mapping.FirstOrDefault(x => x.Value == column).Key ?? column;
}

/// <summary>
/// A clear name given to a stored column, as a list rather than a dictionary where the names are
/// written by hand : an editor adds items to a list, while the keys of a dictionary belong to its
/// schema.
/// </summary>
public class ColumnMapping
{
    /// <summary>The name the workflow reads the column by.</summary>
    public string Name { get; set; } = "";

    /// <summary>The stored column, or a name it is already mapped under.</summary>
    public string Column { get; set; } = "";
}

/// <summary>
/// References to the row of a table a task iterates on : "$row.index" (from 0) and
/// "$row.columns.name" (or "$row.columns['unit price']"). Only the task knows the row, so the engine
/// leaves these references untouched for it to resolve, in the properties marked as evaluated per row
/// with the property of the input holding the table :
/// <c>[JsonSchemaExtensionData(RowScope.SchemaKey, nameof(Source))]</c>.
/// <para>
/// Knowing the table, the engine checks the columns against its mapping (see
/// <see cref="TableRef.Mapping"/>) : a mapped table is read by its names, a column it doesn't name
/// is refused before the task runs, and an editor offers the ones it does. A table without a
/// mapping is left to the task, its columns being only known once it runs.
/// </para>
/// </summary>
public static class RowScope
{
    public const string Root = "$row";
    public const string Index = $"{Root}.index";
    private const string Columns = $"{Root}.columns";

    public const string SchemaKey = "x-fantoche-scope";

    /// <summary>
    /// The reference to the column [name], with brackets when it can't be written after a dot.
    /// </summary>
    public static string ColumnReference(string name)
    {
        bool isPlain = name.Length > 0 && name.All(x => char.IsLetterOrDigit(x) || x == '_');
        return isPlain ? $"{Columns}.{name}" : $"{Columns}['{name}']";
    }

    public static bool IsReference(string value)
        => value == Root || value.StartsWith($"{Root}.") || value.StartsWith($"{Root}[");

    /// <summary>
    /// The column [reference] points at, as "$row.columns.name" or "$row.columns['unit price']".
    /// Only called on references other than <see cref="Index"/>.
    /// </summary>
    public static string ColumnOf(string reference)
    {
        string path = reference.StartsWith(Columns) ? reference[Columns.Length..] : "";
        if (path.Length > 1 && path.StartsWith('.'))
            return path[1..];
        if (path.Length > 4 && path.StartsWith("['") && path.EndsWith("']"))
            return path[2..^2];

        throw new ArgumentException($"[{reference}] is not a row reference, expected \"{Index}\" or \"{Columns}.<name>\".");
    }
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
    /// Create a table from the result of [query], a DuckDB SQL select reading other tables by their
    /// <see cref="TableRef.Id"/>, e.g. <c>$"SELECT * FROM {source.Id} WHERE amount > $min"</c>.
    /// </summary>
    Task<TableRef> CreateTableAsync(string query, IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellation = default);

    /// <summary>
    /// Create a table per query of [queries], in order, as <see cref="CreateTableAsync"/> does : the
    /// tables they read are loaded once for all of them, which is what splitting a large table
    /// into many needs. Every query is given [parameters].
    /// </summary>
    Task<List<TableRef>> CreateTablesAsync(IReadOnlyList<string> queries, IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellation = default);

    /// <summary>
    /// How many rows [table] holds, read from the stored file without loading its rows.
    /// </summary>
    Task<long> CountRowsAsync(TableRef table, CancellationToken cancellation = default);

    /// <summary>
    /// Rows of [table] in order, by column name.
    /// </summary>
    IAsyncEnumerable<IReadOnlyDictionary<string, object?>> ReadRowsAsync(TableRef table,
        CancellationToken cancellation = default);

    Task<FileRef> SaveFileAsync(string name, Stream content, CancellationToken cancellation = default);

    Task<Stream> OpenFileAsync(FileRef file, CancellationToken cancellation = default);
}
