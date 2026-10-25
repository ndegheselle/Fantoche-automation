using Fantoche.Plugins.Shared;

namespace Fantoche.Plugins.Tables;

public class ImportCsvParameters
{
    /// <summary>
    /// Path of the CSV file on the worker.
    /// </summary>
    public string Path { get; set; } = "";
}

/// <summary>
/// Load a CSV file into a table of the storage, its delimiter, columns and their types being
/// detected from the content. The columns keep the names of the file : give them clear ones with
/// <see cref="PickColumnsTask"/> and <see cref="MapColumnsTask"/>.
/// </summary>
public class ImportCsvTask : BaseTask<ImportCsvParameters, TableRef>
{
    public override async Task<TableRef> DoAsync(ImportCsvParameters parameters, ITaskRuntime runtime, CancellationToken? cancellation = null)
    {
        CancellationToken token = cancellation ?? CancellationToken.None;
        Dictionary<string, object?> values = new() { ["path"] = parameters.Path };
        TableRef table = await runtime.Storage.CreateTableAsync("SELECT * FROM read_csv_auto($path)", values, token);
        long rows = await runtime.Storage.CountRowsAsync(table, token);

        runtime.Progress?.Report(new TaskNotification
        {
            State = EnumTaskNotificationState.Success,
            Message = $"Imported {rows} rows and {table.Columns.Count} columns from [{parameters.Path}]"
        });
        return table;
    }
}
