using Fantoche.Plugins.Shared;

namespace Fantoche.Plugins.Tables;

public class SplitParameters
{
    public TableRef Source { get; set; } = new();
    public int BatchSize { get; set; } = 10000;
}

public class SplitResult
{
    /// <summary>The rows of the source in order, [BatchSize] at most per table.</summary>
    public List<TableRef> Batches { get; set; } = [];
}

/// <summary>
/// Cut a large table into tables of a few rows each, for the tasks handling a table at once (a PDF
/// per table, for instance) : no batch of the output is ever larger than what they can handle.
/// </summary>
public class SplitTask : BaseTask<SplitParameters, SplitResult>
{
    public override async Task<SplitResult> DoAsync(SplitParameters parameters, ITaskRuntime runtime, CancellationToken? cancellation = null)
    {
        CancellationToken token = cancellation ?? CancellationToken.None;
        if (parameters.BatchSize <= 0)
            throw new ArgumentException($"A batch can't hold {parameters.BatchSize} rows.");

        TableRef source = parameters.Source;
        long rows = await runtime.Storage.CountRowsAsync(source, token);
        string columns = string.Join(", ", Sql.NamedColumns(source));

        List<string> queries = [];
        for (long start = 0; start < rows; start += parameters.BatchSize)
        {
            long end = start + parameters.BatchSize;
            queries.Add($"SELECT {columns} FROM {Sql.Quote(source.Id)} WHERE rowid >= {start} AND rowid < {end} ORDER BY rowid");
        }

        // Created at once : the source is loaded once for all of the batches.
        List<TableRef> batches = queries.Count == 0 ? [] : await runtime.Storage.CreateTablesAsync(queries, cancellation: token);
        foreach (TableRef batch in batches)
        {
            batch.Name = source.Name;
            batch.Mapping = Sql.StoredMapping(source);
        }

        runtime.Progress?.Report(new TaskNotification
        {
            State = EnumTaskNotificationState.Success,
            Message = $"Split {rows} rows into {batches.Count} batches"
        });
        return new SplitResult { Batches = batches };
    }
}
