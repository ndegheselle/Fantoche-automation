using Fantoche.Plugins.Shared;
using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace Fantoche.Shared.Data;

/// <summary>
/// The tables the properties evaluated per row read their rows from (see <see cref="RowScope"/>) :
/// what the row references are checked against, and what an editor offers.
/// </summary>
public static class RowTables
{
    /// <summary>
    /// Every property of [schema] evaluated per row, with the property of the input holding its
    /// table. A table property the input doesn't have leaves the references unchecked.
    /// </summary>
    public static IReadOnlyDictionary<string, string> Of(JsonSchema? schema)
    {
        if (schema == null)
            return new Dictionary<string, string>();

        Dictionary<string, string> tables = [];
        foreach (var (name, property) in schema.ActualSchema.ActualProperties)
        {
            if (property.ExtensionData?.TryGetValue(RowScope.SchemaKey, out object? table) == true)
                tables[name] = table as string ?? "";
        }
        return tables;
    }

    /// <summary>
    /// The names the table at [tableProperty] of [input] is read by : the ones of its mapping,
    /// empty when it isn't mapped or isn't there.
    /// </summary>
    public static IReadOnlyList<string> NamesOf(JToken input, string tableProperty)
    {
        if (input is not JObject values || values[tableProperty]?[nameof(TableRef.Mapping)] is not JObject mapping)
            return [];
        return [.. mapping.Properties().Select(x => x.Name)];
    }

    /// <summary>
    /// What is wrong with the row references of [input], a resolved mapping : a column the mapping
    /// of their table doesn't name.
    /// </summary>
    public static List<string> UnknownColumns(JToken input, IReadOnlyDictionary<string, string> tables)
    {
        List<string> errors = [];
        foreach (var (property, tableProperty) in tables)
        {
            IReadOnlyList<string> names = NamesOf(input, tableProperty);
            if (names.Count == 0 || input[property] is not JToken scoped)
                continue;

            foreach (string reference in RowReferencesIn(scoped))
            {
                if (ColumnOf(reference, errors) is not string column || names.Contains(column))
                    continue;
                errors.Add($"[{reference}] : [{tableProperty}] names no column [{column}], only {string.Join(", ", names)}.");
            }
        }
        return errors;
    }

    /// <summary>
    /// The references to the columns [template] can read per row, the mapping of a node resolved
    /// in every context of [contexts] : the names of the mapping of each table it reads rows from.
    /// </summary>
    public static IReadOnlyList<string> Offered(JToken template, IEnumerable<JObject> contexts, JsonSchema? schema)
    {
        IReadOnlyDictionary<string, string> tables = Of(schema);
        if (tables.Count == 0)
            return [];

        HashSet<string> references = [];
        foreach (JObject context in contexts)
        {
            // Resolving replaces the references in place, so each context gets its own copies.
            JToken resolved = ReferencesHandler.ReplaceReferences(template.DeepClone(), context.DeepClone(), schema).Replaced;
            IEnumerable<string> names = tables.Values.Distinct().SelectMany(table => NamesOf(resolved, table));
            references.UnionWith(names.Select(RowScope.ColumnReference));
        }
        return [.. references];
    }

    private static IEnumerable<string> RowReferencesIn(JToken token)
    {
        IEnumerable<JToken> tokens = token is JContainer container ? container.DescendantsAndSelf() : [token];
        return tokens
            .Where(x => x.Type == JTokenType.String)
            .Select(x => x.Value<string>()!)
            .Where(x => RowScope.IsReference(x) && x != RowScope.Index);
    }

    /// <summary>The column [reference] reads, null with the reason added to [errors] when it reads none.</summary>
    private static string? ColumnOf(string reference, List<string> errors)
    {
        try
        {
            return RowScope.ColumnOf(reference);
        }
        catch (ArgumentException exception)
        {
            errors.Add(exception.Message);
            return null;
        }
    }
}
