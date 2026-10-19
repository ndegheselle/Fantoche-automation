using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Remote;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json.Linq;

namespace Fantoche.Supervisor.Api.Execution;

public class CallerHub : Hub<ICallerClient>, ICallerHub
{
    private readonly Dispatcher _dispatcher;

    public CallerHub(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public Task Execute(TaskInstance instance, JToken? globalContext) => _dispatcher.SubmitAsync(Context.ConnectionId, instance, globalContext);

    public Task Cancel(Guid executionId) => _dispatcher.CancelAsync(Context.ConnectionId, executionId);

    public override Task OnDisconnectedAsync(Exception? exception) => _dispatcher.RemoveCallerAsync(Context.ConnectionId);
}
