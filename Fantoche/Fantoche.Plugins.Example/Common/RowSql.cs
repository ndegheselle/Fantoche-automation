using System.Text.Json.Serialization;
using Fantoche.Plugins.Shared;

namespace Fantoche.Plugins.Example.Common;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EnumConditionOperator
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
/// Compare two operands, each one a value or a reference to the row (see <see cref="RowScope"/>) :
/// "$row.columns.name" reads the column a name of the mapping stands for, or the stored one.
/// </summary>
public class RowCondition
{
    public object? Left { get; set; }
    public EnumConditionOperator Operator { get; set; }
    public object? Right { get; set; }
}

/// <summary>
/// Row conditions and values compiled to SQL rather than evaluated row by row : the storage runs
/// them on the whole table at once, which is what keeps a large table fast.
/// </summary>
internal sealed class RowSql
{
    private readonly TableRef _table;

    /// <summary>The values the compiled SQL refers to, as parameters of the query.</summary>
    public Dictionary<string, object?> Values { get; } = [];

    public RowSql(TableRef table)
    {
        _table = table;
    }

    /// <summary>
    /// The SQL holding when every condition does ([matchAll]) or any of them does. True without
    /// conditions.
    /// </summary>
    public string Where(IReadOnlyList<RowCondition> conditions, bool matchAll)
    {
        if (conditions.Count == 0)
            return "true";

        IEnumerable<string> compiled = conditions.Select(Condition);
        return string.Join(matchAll ? " AND " : " OR ", compiled);
    }

    /// <summary>
    /// [operand] as SQL : a row reference becomes the column it points at, anything else a parameter.
    /// </summary>
    public string Operand(object? operand)
    {
        if (operand is string reference && RowScope.IsReference(reference))
            return RowReference(reference);

        string name = $"p{Values.Count}";
        Values[name] = operand;
        return $"${name}";
    }

    private string Condition(RowCondition condition)
    {
        string left = Operand(condition.Left);
        string right = Operand(condition.Right);

        return condition.Operator switch
        {
            EnumConditionOperator.Equal => $"({left} = {right})",
            EnumConditionOperator.NotEqual => $"({left} <> {right})",
            EnumConditionOperator.Greater => $"({left} > {right})",
            EnumConditionOperator.GreaterOrEqual => $"({left} >= {right})",
            EnumConditionOperator.Less => $"({left} < {right})",
            EnumConditionOperator.LessOrEqual => $"({left} <= {right})",
            EnumConditionOperator.Contains => $"contains(CAST({left} AS VARCHAR), CAST({right} AS VARCHAR))",
            _ => throw new ArgumentException($"Unknown operator [{condition.Operator}].")
        };
    }

    private string RowReference(string reference)
    {
        if (reference == RowScope.Index)
            return "rowid";

        string name = RowScope.ColumnOf(reference);
        string column = Sql.ColumnOf(_table, name);
        return Sql.Quote(column);
    }
}
