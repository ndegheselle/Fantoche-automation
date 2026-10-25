using Fantoche.Plugins.Shared;
using Newtonsoft.Json.Linq;

namespace Fantoche.Worker.Tests.Tables;

/// <summary>
/// The CSV files a test imports, in a folder of its own removed with it, and what its tables hold.
/// </summary>
internal sealed class CsvFiles : IDisposable
{
    public string Folder { get; } = Path.Combine(Path.GetTempPath(), "fantoche-tests", Guid.NewGuid().ToString("N"));

    public CsvFiles()
    {
        Directory.CreateDirectory(Folder);
    }

    public void Dispose() => Directory.Delete(Folder, true);

    /// <summary>Write [content] as the file [name] and return its path.</summary>
    public string Write(string name, string content)
    {
        string path = Path.Combine(Folder, name);
        File.WriteAllText(path, content);
        return path;
    }

    /// <summary>The rows of the table [table] holds, read from the storage of [run].</summary>
    public static async Task<List<IReadOnlyDictionary<string, object?>>> RowsAsync(WorkflowRun run, JToken? table)
    {
        TableRef handle = table!.ToObject<TableRef>()!;
        List<IReadOnlyDictionary<string, object?>> rows = [];
        await foreach (var row in run.Storage.ReadRowsAsync(handle))
            rows.Add(row);
        return rows;
    }

    /// <summary>The values of [column] in the rows of [table], in order.</summary>
    public static async Task<List<object?>> ColumnAsync(WorkflowRun run, JToken? table, string column)
    {
        List<IReadOnlyDictionary<string, object?>> rows = await RowsAsync(run, table);
        return [.. rows.Select(row => row[column])];
    }
}
