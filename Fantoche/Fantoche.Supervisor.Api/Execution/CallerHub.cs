using Fantoche.Services.Local;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Remote;
using Microsoft.AspNetCore.SignalR;

namespace Fantoche.Supervisor.Api.Execution;

/// <summary>
/// The callers of the <see cref="Dispatcher"/>, each known by its connection.
/// </summary>
public class CallerHub : Hub<ICallerClient>, ICallerHub
{
    private readonly Dispatcher _dispatcher;
    private readonly IHubContext<CallerHub, ICallerClient> _callers;

    public CallerHub(Dispatcher dispatcher, IHubContext<CallerHub, ICallerClient> callers)
    {
        _dispatcher = dispatcher;
        _callers = callers;
    }

    /// <summary>
    /// The caller is reached through the context of the hub rather than through this one, which
    /// only lives for the call.
    /// </summary>
    public Task Execute(TaskInstance instance)
        => _dispatcher.SubmitAsync(Context.ConnectionId, _callers.Clients.Client(Context.ConnectionId), instance);

    public Task Cancel(Guid executionId) => _dispatcher.CancelAsync(Context.ConnectionId, executionId);

    public override Task OnDisconnectedAsync(Exception? exception) => _dispatcher.RemoveCallerAsync(Context.ConnectionId);
}
