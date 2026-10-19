using Fantoche.Shared.Remote;
using Microsoft.AspNetCore.SignalR;

namespace Fantoche.Supervisor.Api.Execution;

public class CallerHub : Hub<ICallerClient>, ICallerHub
{
    private readonly Dispatcher _dispatcher;

    public CallerHub(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public Task Execute(ExecutionRequest request) => _dispatcher.SubmitAsync(Context.ConnectionId, request);

    public Task Cancel(Guid executionId) => _dispatcher.CancelAsync(Context.ConnectionId, executionId);

    public override Task OnDisconnectedAsync(Exception? exception) => _dispatcher.RemoveCallerAsync(Context.ConnectionId);
}
