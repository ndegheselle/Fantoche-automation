using System.Text.Json.Serialization;
using Fantoche.Plugins.Shared;
using NJsonSchema.Annotations;

namespace Fantoche.Plugins.Tables;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EnumFilterOperator
{
    Equal,
    NotEqual,
    Greater,
    GreaterOrEqual,
    Less,
    LessOrEqual,
    Contains
}

/// <summary>
/// Compare two operands, each one a value or a reference to the row (see <see cref="RowScope"/>).
/// </summary>
public class FilterCondition
{
    public object? Left { get; set; }
    public EnumFilterOperator Operator { get; set; }
    public object? Right { get; set; }
}

public class FilterParameters
{
    public TableRef Source { get; set; } = new();

    [JsonSchemaExtensionData(RowScope.SchemaKey, RowScope.Name)]
    public List<FilterCondition> Conditions { get; set; } = [];

    /// <summary>
    /// Keep a row when every condition holds, otherwise when any of them does.
    /// </summary>
    public bool MatchAll { get; set; } = true;
}

/// <summary>
/// Create a table with the rows of the source that match the conditions, in the same order.
/// <para>
/// The conditions are compiled to a SQL filter run by the storage rather than evaluated row by row :
/// the values are passed as parameters and the columns quoted, so nothing of the settings reaches the
/// query as SQL.
/// </para>
/// </summary>
public class FilterTask : BaseTask<FilterParameters, TableRef>
{
    public override Task<TableRef> DoAsync(FilterParameters parameters, ITaskRuntime runtime, CancellationToken? cancellation = null)
    {
        Dictionary<string, object?> values = [];
        List<string> conditions = [];
        foreach (FilterCondition condition in parameters.Conditions)
            conditions.Add(ConditionSql(condition, values));

        string where = conditions.Count == 0
            ? "true"
            : string.Join(parameters.MatchAll ? " AND " : " OR ", conditions);
        string query = $"SELECT * FROM {Quote(parameters.Source.Id)} WHERE {where} ORDER BY rowid";

        return runtime.Storage.CreateTableAsync(query, values, cancellation ?? CancellationToken.None);
    }

    private static string ConditionSql(FilterCondition condition, Dictionary<string, object?> values)
    {
        string left = OperandSql(condition.Left, values);
        string right = OperandSql(condition.Right, values);

        return condition.Operator switch
        {
            EnumFilterOperator.Equal => $"({left} = {right})",
            EnumFilterOperator.NotEqual => $"({left} <> {right})",
            EnumFilterOperator.Greater => $"({left} > {right})",
            EnumFilterOperator.GreaterOrEqual => $"({left} >= {right})",
            EnumFilterOperator.Less => $"({left} < {right})",
            EnumFilterOperator.LessOrEqual => $"({left} <= {right})",
            EnumFilterOperator.Contains => $"contains(CAST({left} AS VARCHAR), CAST({right} AS VARCHAR))",
            _ => throw new ArgumentException($"Unknown operator [{condition.Operator}].")
        };
    }

    /// <summary>
    /// [operand] as SQL : a row reference becomes the column it points at, anything else a parameter
    /// added to [values].
    /// </summary>
    private static string OperandSql(object? operand, Dictionary<string, object?> values)
    {
        if (operand is string reference && RowScope.IsReference(reference))
            return RowReferenceSql(reference);

        string name = $"p{values.Count}";
        values[name] = operand;
        return $"${name}";
    }

    private static string RowReferenceSql(string reference)
    {
        if (reference == RowReference.Index)
            return "rowid";

        string column = RowReference.ColumnOf(reference);
        return Quote(column);
    }

    private static string Quote(string identifier) => $"\"{identifier.Replace("\"", "\"\"")}\"";
}
