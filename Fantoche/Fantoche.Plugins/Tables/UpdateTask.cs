using Fantoche.Plugins.Shared;
using NJsonSchema.Annotations;

namespace Fantoche.Plugins.Tables;

/// <summary>The value a column is given : a value or a reference to the row (see <see cref="RowScope"/>).</summary>
public class ColumnValue
{
    /// <summary>A column of the table, or the name of a column to add.</summary>
    public string Column { get; set; } = "";
    public object? Value { get; set; }
}

public class UpdateParameters
{
    [JsonSchemaExtensionData(OutputPreview.SchemaKey, OutputPreview.Whole)]
    public TableRef Source { get; set; } = new();

    /// <summary>The rows updated, every row without conditions.</summary>
    [JsonSchemaExtensionData(RowScope.SchemaKey, RowScope.Name)]
    public List<RowCondition> Conditions { get; set; } = [];

    /// <summary>
    /// Update a row when every condition holds, otherwise when any of them does.
    /// </summary>
    public bool MatchAll { get; set; } = true;

    [JsonSchemaExtensionData(RowScope.SchemaKey, RowScope.Name)]
    public List<ColumnValue> Set { get; set; } = [];
}

/// <summary>
/// Create a table with the rows of the source matching the conditions updated : a column of the
/// table is given a value, or a column is added, empty for the rows left as they are. The rows keep
/// their order, and the columns their names.
/// </summary>
public class UpdateTask : BaseTask<UpdateParameters, TableRef>
{
    public override async Task<TableRef> DoAsync(UpdateParameters parameters, ITaskRuntime runtime, CancellationToken? cancellation = null)
    {
        CancellationToken token = cancellation ?? CancellationToken.None;
        TableRef source = parameters.Source;
        RowSql sql = new(source);
        string where = sql.Where(parameters.Conditions, parameters.MatchAll);

        // The values by the stored column they update, the added columns by their name.
        Dictionary<string, string> updated = [];
        List<(string Name, string Value)> added = [];
        foreach (ColumnValue set in parameters.Set)
        {
            if (string.IsNullOrWhiteSpace(set.Column))
                throw new ArgumentException("A value is given to no column.");

            string value = sql.Operand(set.Value);
            string column = source.ColumnOf(set.Column);
            if (source.Columns.Any(x => x.Name == column))
                updated[column] = value;
            else
                added.Add((set.Column, value));
        }

        List<string> columns = [];
        foreach (TableColumn column in source.Columns)
        {
            string name = Sql.Quote(source.NameOf(column.Name));
            string stored = Sql.Quote(column.Name);
            columns.Add(updated.TryGetValue(column.Name, out string? value)
                ? $"CASE WHEN {where} THEN {value} ELSE {stored} END AS {name}"
                : $"{stored} AS {name}");
        }
        columns.AddRange(added.Select(x => $"CASE WHEN {where} THEN {x.Value} END AS {Sql.Quote(x.Name)}"));

        string query = $"SELECT {string.Join(", ", columns)} FROM {Sql.Quote(source.Id)} ORDER BY rowid";
        TableRef table = await runtime.Storage.CreateTableAsync(query, sql.Values, token);

        // The added columns are named by the workflow as the mapped ones are.
        table.Mapping = Sql.StoredMapping(source);
        foreach (var (name, _) in added)
            table.Mapping[name] = name;

        runtime.Progress?.Report(new TaskNotification
        {
            State = EnumTaskNotificationState.Success,
            Message = $"Updated {updated.Count} columns and added {added.Count}"
        });
        return table;
    }
}
