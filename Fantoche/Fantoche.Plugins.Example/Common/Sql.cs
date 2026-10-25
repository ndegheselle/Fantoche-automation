using Fantoche.Plugins.Shared;

namespace Fantoche.Plugins.Example.Common;

/// <summary>
/// What the table tasks build their queries with. Nothing of the settings reaches a query as SQL :
/// the identifiers are quoted and the values passed as parameters.
/// </summary>
internal static class Sql
{
    public static string Quote(string identifier) => $"\"{identifier.Replace("\"", "\"\"")}\"";

    /// <summary>
    /// The columns of [table] under the names the workflow reads them by (see
    /// <see cref="TableRef.Mapping"/>), read from [alias] when the query names the table.
    /// </summary>
    public static List<string> NamedColumns(TableRef table, string? alias = null)
    {
        string prefix = alias == null ? "" : $"{alias}.";
        return [.. table.Columns.Select(column => $"{prefix}{Quote(column.Name)} AS {Quote(table.NameOf(column.Name))}")];
    }

    /// <summary>
    /// The mapping of a table created from the <see cref="NamedColumns"/> of [sources] : the mapped
    /// columns are stored under their names, each one now standing for itself.
    /// </summary>
    public static Dictionary<string, string> StoredMapping(params TableRef[] sources)
    {
        Dictionary<string, string> mapping = [];
        foreach (string name in sources.SelectMany(source => source.Mapping.Keys))
            mapping[name] = name;
        return mapping;
    }

    /// <summary>
    /// The stored column of [table] that [name] stands for, refused when there is none : a query
    /// would fail on it too, naming neither the table nor what it holds.
    /// </summary>
    public static string ColumnOf(TableRef table, string name)
    {
        string column = table.ColumnOf(name);
        if (table.Columns.Any(x => x.Name == column))
            return column;

        string known = string.Join(", ", table.Columns.Select(x => table.NameOf(x.Name)));
        throw new ArgumentException($"Column [{name}] not found in the table, which holds : {known}.");
    }
}
