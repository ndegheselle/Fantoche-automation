using Fantoche.Plugins.Shared;

namespace Fantoche.Plugins.Tables;

public class CsvImportParameters
{
    /// <summary>
    /// Path of the CSV file on the worker.
    /// </summary>
    public string Path { get; set; } = "";
}

/// <summary>
/// Load a CSV file into a table of the storage, its columns and their types being detected from the
/// content.
/// </summary>
public class CsvImportTask : BaseTask<CsvImportParameters, TableRef>
{
    public override async Task<TableRef> DoAsync(CsvImportParameters parameters, ITaskRuntime runtime, CancellationToken? cancellation = null)
    {
        // DuckDB detects the columns and their types from the content.
        Dictionary<string, object?> values = new() { ["path"] = parameters.Path };
        TableRef table = await runtime.Storage.CreateTableAsync("SELECT * FROM read_csv_auto($path)", values, cancellation ?? CancellationToken.None);

        runtime.Progress?.Report(new TaskNotification
        {
            State = EnumTaskNotificationState.Success,
            Message = $"Imported [{parameters.Path}] with {table.Columns.Count} columns"
        });
        return table;
    }
}
