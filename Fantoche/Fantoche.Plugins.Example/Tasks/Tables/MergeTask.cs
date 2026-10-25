using System.Text.Json.Serialization;
using Fantoche.Plugins.Example.Common;
using Fantoche.Plugins.Shared;
using NJsonSchema.Annotations;

namespace Fantoche.Plugins.Example.Tasks.Tables;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EnumMergeKind
{
    /// <summary>Only the rows matching on both sides.</summary>
    Inner,

    /// <summary>Every row of the left table, the ones matching nothing with empty right columns.</summary>
    Left
}

/// <summary>A column of each table, the rows merged when their values are equal.</summary>
public class MergeKey
{
    public string Left { get; set; } = "";
    public string Right { get; set; } = "";
}

public class MergeParameters
{
    [JsonSchemaExtensionData(OutputPreview.SchemaKey, OutputPreview.Whole)]
    public TableRef Left { get; set; } = new();

    [JsonSchemaExtensionData(OutputPreview.SchemaKey, OutputPreview.Whole)]
    public TableRef Right { get; set; } = new();

    /// <summary>The rows are merged when every pair holds the same value.</summary>
    public List<MergeKey> On { get; set; } = [];

    public EnumMergeKind Kind { get; set; } = EnumMergeKind.Inner;
}

/// <summary>
/// Merge two tables on the columns they share, as a SQL join : a row per pair of rows matching, in
/// the order of the left table then of the right one. The columns of both tables are kept under
/// their names, the right columns of the keys being left out as they repeat the left ones. A right
/// column whose name the left table already uses is prefixed with the name of the right table
/// ("customers_Name"), the merged table keeping the name of the left one.
/// </summary>
public class MergeTask : BaseTask<MergeParameters, TableRef>
{
    private const string LeftAlias = "l";
    private const string RightAlias = "r";

    /// <summary>What tells the right columns apart when the right table has no name.</summary>
    private const string UnnamedRight = "Right";

    public override async Task<TableRef> DoAsync(MergeParameters parameters, ITaskRuntime runtime, CancellationToken? cancellation = null)
    {
        CancellationToken token = cancellation ?? CancellationToken.None;
        if (parameters.On.Count == 0)
            throw new ArgumentException("No column to merge the tables on.");

        TableRef left = parameters.Left;
        TableRef right = parameters.Right;
        List<(string Left, string Right)> keys = [.. parameters.On.Select(key => (Sql.ColumnOf(left, key.Left), Sql.ColumnOf(right, key.Right)))];

        List<string> columns = Sql.NamedColumns(left, LeftAlias);
        Dictionary<string, string> mapping = Sql.StoredMapping(left);
        HashSet<string> names = new(left.Columns.Select(x => left.NameOf(x.Name)), StringComparer.OrdinalIgnoreCase);
        HashSet<string> rightKeys = [.. keys.Select(x => x.Right)];
        string prefix = string.IsNullOrWhiteSpace(right.Name) ? UnnamedRight : right.Name;
        foreach (TableColumn column in right.Columns.Where(x => !rightKeys.Contains(x.Name)))
        {
            string name = right.NameOf(column.Name);
            string output = names.Contains(name) ? $"{prefix}_{name}" : name;
            if (!names.Add(output))
                throw new ArgumentException($"The column [{output}] is in both tables : map one of them under another name.");

            columns.Add($"{RightAlias}.{Sql.Quote(column.Name)} AS {Sql.Quote(output)}");
            if (right.Mapping.ContainsKey(name))
                mapping[output] = output;
        }

        string join = parameters.Kind == EnumMergeKind.Left ? "LEFT JOIN" : "INNER JOIN";
        IEnumerable<string> on = keys.Select(key => $"{LeftAlias}.{Sql.Quote(key.Left)} = {RightAlias}.{Sql.Quote(key.Right)}");
        string query = $"""
            SELECT {string.Join(", ", columns)}
            FROM {Sql.Quote(left.Id)} {LeftAlias} {join} {Sql.Quote(right.Id)} {RightAlias} ON {string.Join(" AND ", on)}
            ORDER BY {LeftAlias}.rowid, {RightAlias}.rowid
            """;

        TableRef merged = await runtime.Storage.CreateTableAsync(query, cancellation: token);
        merged.Name = left.Name;
        merged.Mapping = mapping;

        long rows = await runtime.Storage.CountRowsAsync(merged, token);
        runtime.Progress?.Report(new TaskNotification
        {
            State = EnumTaskNotificationState.Success,
            Message = $"Merged into {rows} rows"
        });
        return merged;
    }
}
