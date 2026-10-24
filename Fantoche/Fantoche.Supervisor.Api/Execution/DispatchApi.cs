using Fantoche.Services.Local;
using Fantoche.Shared.Remote;
using Microsoft.AspNetCore.SignalR;

namespace Fantoche.Supervisor.Api.Execution;

/// <summary>
/// Pushes every change of what the supervisor dispatches to the applications.
/// </summary>
public class DispatchHub : Hub<IDispatchClient>
{
}

/// <summary>
/// The workers and the executions they run or wait for, see <see cref="Shared.Services.IDispatchService"/>.
/// </summary>
public static class DispatchApi
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/dispatch/status", (Dispatcher dispatcher) => dispatcher.GetStatus());
        app.MapPost("/dispatch/runs/{id:guid}/resume", (Dispatcher dispatcher, Guid id) => dispatcher.ResumeAsync(id));

        app.MapHub<DispatchHub>($"/{SupervisorRoutes.Dispatch}");

        Dispatcher dispatcher = app.Services.GetRequiredService<Dispatcher>();
        IHubContext<DispatchHub, IDispatchClient> hub = app.Services.GetRequiredService<IHubContext<DispatchHub, IDispatchClient>>();
        dispatcher.StatusChanged += status => _ = hub.Clients.All.StatusChanged(status);
    }
}
