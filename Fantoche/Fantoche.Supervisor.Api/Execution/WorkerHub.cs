using Fantoche.Plugins.Shared;
using Fantoche.Services.Local;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Remote;
using Microsoft.AspNetCore.SignalR;

namespace Fantoche.Supervisor.Api.Execution;

/// <summary>
/// The workers of the <see cref="Dispatcher"/>, each known by its connection.
/// </summary>
public class WorkerHub : Hub<IWorkerClient>, IWorkerHub
{
    private readonly Dispatcher _dispatcher;
    private readonly IHubContext<WorkerHub, IWorkerClient> _workers;

    public WorkerHub(Dispatcher dispatcher, IHubContext<WorkerHub, IWorkerClient> workers)
    {
        _dispatcher = dispatcher;
        _workers = workers;
    }

    /// <summary>See <see cref="CallerHub.Execute"/> for how the worker is reached.</summary>
    public Task Register(string workerId, int capacity)
        => _dispatcher.AddWorkerAsync(Context.ConnectionId, _workers.Clients.Client(Context.ConnectionId), workerId, capacity);

    public Task ReportState(Guid executionId, TaskInstance instance) => _dispatcher.ReportStateAsync(Context.ConnectionId, executionId, instance);

    public Task ReportNotification(Guid executionId, TaskNotification notification) => _dispatcher.ReportNotificationAsync(executionId, notification);

    public Task Complete(TaskInstance instance) => _dispatcher.CompleteAsync(Context.ConnectionId, instance);

    public override Task OnDisconnectedAsync(Exception? exception) => _dispatcher.RemoveWorkerAsync(Context.ConnectionId);
}
