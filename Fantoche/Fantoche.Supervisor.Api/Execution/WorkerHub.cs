using Fantoche.Plugins.Shared;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Remote;
using Microsoft.AspNetCore.SignalR;

namespace Fantoche.Supervisor.Api.Execution;

public class WorkerHub : Hub<IWorkerClient>, IWorkerHub
{
    private readonly Dispatcher _dispatcher;

    public WorkerHub(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public Task Register(string workerId, int capacity) => _dispatcher.AddWorkerAsync(Context.ConnectionId, workerId, capacity);

    public Task ReportState(Guid executionId, TaskInstance instance) => _dispatcher.ReportStateAsync(instance);

    public Task ReportNotification(Guid executionId, TaskNotification notification) => _dispatcher.ReportNotificationAsync(executionId, notification);

    public Task Complete(TaskInstance instance) => _dispatcher.CompleteAsync(Context.ConnectionId, instance);

    public override Task OnDisconnectedAsync(Exception? exception) => _dispatcher.RemoveWorkerAsync(Context.ConnectionId);
}
