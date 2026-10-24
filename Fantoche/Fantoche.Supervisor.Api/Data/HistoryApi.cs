using Fantoche.Services.Local;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Remote;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json.Linq;

namespace Fantoche.Supervisor.Api.Data;

/// <summary>
/// Pushes every change of the history to the applications, whoever made it : an application, a
/// worker or the supervisor.
/// </summary>
public class HistoryHub : Hub<IHistoryClient>
{
}

/// <summary>
/// The instances the tasks ran with, see <see cref="Shared.Services.IHistoryService"/>.
/// </summary>
public static class HistoryApi
{
    public static void Map(WebApplication app)
    {
        var history = app.MapGroup("/history");

        history.MapGet("/elements/{id:guid}", (LocalHistoryService service, Guid id, int? page, int? pageSize)
            => service.GetByScopedAsync(id, ScopedApi.Paging(page, pageSize)));
        history.MapGet("/instances/{id:guid}", async (LocalHistoryService service, Guid id) =>
        {
            TaskInstance? instance = await service.GetAsync(id);
            return instance == null ? Results.NotFound() : TypedResults.Ok(instance);
        });
        history.MapGet("/instances/{id:guid}/children", (LocalHistoryService service, Guid id) => service.GetChildrenAsync(id));
        history.MapGet("/instances/{id:guid}/descendants", (LocalHistoryService service, Guid id) => service.GetDescendantsAsync(id));
        history.MapGet("/runs/unfinished", (LocalHistoryService service, string? applicationId) => service.GetUnfinishedRunsAsync(applicationId));
        history.MapPost("/instances/{id:guid}/answer", (LocalHistoryService service, Guid id, JToken answer) => service.AnswerAsync(id, answer));
        history.MapPost("/runs/{id:guid}/interrupt", (LocalHistoryService service, Guid id) => service.InterruptRunAsync(id));
        history.MapPost("/runs/{id:guid}/claim", (LocalHistoryService service, Guid id, string sessionId) => service.ClaimRunAsync(id, sessionId));
        history.MapPut("/instances", (LocalHistoryService service, TaskInstance instance) => service.SaveAsync(instance));

        app.MapHub<HistoryHub>($"/{SupervisorRoutes.History}");

        LocalHistoryService stored = app.Services.GetRequiredService<LocalHistoryService>();
        IHubContext<HistoryHub, IHistoryClient> hub = app.Services.GetRequiredService<IHubContext<HistoryHub, IHistoryClient>>();
        stored.InstanceAdded += instance => _ = hub.Clients.All.InstanceAdded(instance);
        stored.InstanceUpdated += instance => _ = hub.Clients.All.InstanceUpdated(instance);
    }
}
