using System.Text;
using Fantoche.Plugins.Shared;
using Fantoche.Services.Local;
using Fantoche.Shared.Remote;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fantoche.Supervisor.Api.Data;

/// <summary>
/// The storage of the runs the workers hold, which the supervisor reaches (STORAGE_FOLDER, the
/// volume they share) : what a task asking the user reads and writes from the application it runs
/// in (see <see cref="Shared.Data.Execution.UserInterfaceSwitch"/>). A run is known by its id, the
/// supervisor finding its folder from its history.
/// </summary>
public static class StorageApi
{
    public static void Map(WebApplication app)
    {
        var storage = app.MapGroup("/storage/runs/{runId:guid}");

        storage.MapPost("/tables", async (LocalSupervisor supervisor, Guid runId, CreateTablesRequest request) =>
        {
            IStorage stored = await supervisor.OpenStorageAsync(runId);
            return await stored.CreateTablesAsync(request.Queries, ValuesOf(request.Parameters));
        });
        storage.MapGet("/tables/{tableId}/count", async (LocalSupervisor supervisor, Guid runId, string tableId) =>
        {
            IStorage stored = await supervisor.OpenStorageAsync(runId);
            return await stored.CountRowsAsync(new TableRef { Id = tableId });
        });
        storage.MapGet("/tables/{tableId}/page", async (LocalSupervisor supervisor, Guid runId, string tableId, long offset, int limit) =>
        {
            IStorage stored = await supervisor.OpenStorageAsync(runId);
            TablePage page = await stored.ReadPageAsync(new TableRef { Id = tableId }, offset, limit);
            return new RemoteTablePage { Columns = page.Columns, Total = page.Total, Rows = JArray.FromObject(page.Rows) };
        });
        storage.MapGet("/tables/{tableId}/rows", RowsAsync);
        storage.MapPost("/files", async (LocalSupervisor supervisor, Guid runId, string name, HttpRequest request) =>
        {
            IStorage stored = await supervisor.OpenStorageAsync(runId);
            return await stored.SaveFileAsync(name, request.Body);
        });
        storage.MapGet("/files/{fileId}/{name}", async (LocalSupervisor supervisor, Guid runId, string fileId, string name) =>
        {
            IStorage stored = await supervisor.OpenStorageAsync(runId);
            Stream file = await stored.OpenFileAsync(new FileRef { Id = fileId, Name = name });
            return TypedResults.Stream(file, "application/octet-stream");
        });
    }

    /// <summary>
    /// Every row of the table, a line of JSON each, written as they are read : a table of millions
    /// of rows is never held whole.
    /// </summary>
    private static async Task RowsAsync(LocalSupervisor supervisor, Guid runId, string tableId, HttpContext context)
    {
        IStorage stored = await supervisor.OpenStorageAsync(runId);
        context.Response.ContentType = "application/x-ndjson";
        await using StreamWriter writer = new(context.Response.Body, new UTF8Encoding(false));
        await foreach (var row in stored.ReadRowsAsync(new TableRef { Id = tableId }, context.RequestAborted))
            await writer.WriteLineAsync(JObject.FromObject(row).ToString(Formatting.None));
    }

    private static Dictionary<string, object?> ValuesOf(JObject parameters)
        => parameters.Properties().ToDictionary(x => x.Name, x => x.Value is JValue value ? value.Value : (object?)x.Value.ToString());
}
