using Fantoche.Plugins.Example.Common;
using Fantoche.Plugins.Shared;
using NJsonSchema.Annotations;

namespace Fantoche.Plugins.Example.Tasks.Tables;

public class MapColumnsParameters
{
    [JsonSchemaExtensionData(OutputPreview.SchemaKey, OutputPreview.Whole)]
    public TableRef Source { get; set; } = new();

    /// <summary>The name to give each column, a column being named once.</summary>
    [JsonSchemaExtensionData(OutputPreview.MappingSchemaKey, OutputPreview.Whole)]
    public List<ColumnMapping> Columns { get; set; } = [];
}

/// <summary>
/// Give clear names to the columns of a table : the mapping is added to the table handed over
/// (<see cref="TableRef.Mapping"/>) rather than the table being copied, which costs nothing however
/// large it is. The tasks reading the table read the columns by these names.
/// </summary>
public class MapColumnsTask : BaseTask<MapColumnsParameters, TableRef>
{
    public override Task<TableRef> DoAsync(MapColumnsParameters parameters, ITaskRuntime runtime, CancellationToken? cancellation = null)
    {
        TableRef source = parameters.Source;
        Dictionary<string, string> mapping = new(source.Mapping);

        // DuckDB reads the names regardless of their case : two of them differing only by it would
        // be the same column once stored.
        HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> mappedColumns = [];
        foreach (ColumnMapping column in parameters.Columns)
        {
            if (string.IsNullOrWhiteSpace(column.Name))
                throw new ArgumentException($"The column [{column.Column}] is given no name.");
            if (!names.Add(column.Name))
                throw new ArgumentException($"The name [{column.Name}] is given twice.");

            string stored = Sql.ColumnOf(source, column.Column);
            if (!mappedColumns.Add(stored))
                throw new ArgumentException($"The column [{stored}] is given two names.");

            // A column is read by a single name : the one it had before is replaced.
            foreach (string previous in mapping.Where(x => x.Value == stored).Select(x => x.Key).ToList())
                mapping.Remove(previous);
            mapping[column.Name] = stored;
        }

        CheckNoNameHidesAColumn(source, mapping);

        runtime.Progress?.Report(new TaskNotification
        {
            State = EnumTaskNotificationState.Success,
            Message = $"Mapped {parameters.Columns.Count} columns"
        });
        TableRef mapped = new() { Id = source.Id, Name = source.Name, Columns = source.Columns, Mapping = mapping };
        return Task.FromResult(mapped);
    }

    /// <summary>
    /// A name can't be the one of another column read under its own : the two would clash once the
    /// table is stored again under the names.
    /// </summary>
    private static void CheckNoNameHidesAColumn(TableRef source, Dictionary<string, string> mapping)
    {
        HashSet<string> mapped = [.. mapping.Values];
        IEnumerable<string> unmapped = source.Columns.Select(x => x.Name).Where(x => !mapped.Contains(x));
        foreach (string column in unmapped)
        {
            string? hiding = mapping.Keys.FirstOrDefault(name => string.Equals(name, column, StringComparison.OrdinalIgnoreCase));
            if (hiding != null)
                throw new ArgumentException($"The name [{hiding}] is the one of another column : name that column too, or pick another name.");
        }
    }
}
