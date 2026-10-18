using Fantoche.Plugins.Shared;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Remote;
using Microsoft.AspNetCore.SignalR;

namespace Fantoche.Supervisor.Api;

public record WorkerInfo(string Id, int Capacity, int Running);

/// <summary>
/// Hand the executions of the callers to the workers, and route what the workers report back to the
/// caller of each execution. Everything is kept in memory : an execution only lives as long as the
/// connections of its caller and its worker.
/// </summary>
public class Dispatcher
{
    private sealed class Worker
    {
        public required string ConnectionId { get; init; }
        public required string Id { get; init; }
        public required int Capacity { get; init; }
        public HashSet<Guid> Running { get; } = [];
    }

    private sealed class Execution
    {
        public required string CallerConnectionId { get; init; }
        public required RemoteExecution Request { get; init; }
        /// <summary>Null while it waits for a worker.</summary>
        public string? WorkerConnectionId { get; set; }
    }

    private readonly IHubContext<CallerHub, ICallerClient> _callers;
    private readonly IHubContext<WorkerHub, IWorkerClient> _workers;

    private readonly object _lock = new();
    private readonly Dictionary<string, Worker> _workersByConnection = [];
    private readonly Dictionary<Guid, Execution> _executions = [];
    /// <summary>Executions waiting for a worker, the canceled ones being skipped once dequeued.</summary>
    private readonly Queue<Guid> _queue = [];

    public Dispatcher(IHubContext<CallerHub, ICallerClient> callers, IHubContext<WorkerHub, IWorkerClient> workers)
    {
        _callers = callers;
        _workers = workers;
    }

    public IReadOnlyList<WorkerInfo> GetWorkers()
    {
        lock (_lock)
            return [.. _workersByConnection.Values.Select(x => new WorkerInfo(x.Id, x.Capacity, x.Running.Count))];
    }

    #region Callers

    public Task SubmitAsync(string callerConnectionId, RemoteExecution request)
    {
        Guid executionId = request.Instance.Id;
        lock (_lock)
        {
            _executions[executionId] = new Execution { CallerConnectionId = callerConnectionId, Request = request };
            _queue.Enqueue(executionId);
        }
        return DispatchAsync();
    }

    public async Task CancelAsync(string callerConnectionId, Guid executionId)
    {
        Execution? execution;
        string? workerConnectionId;
        lock (_lock)
        {
            if (!_executions.TryGetValue(executionId, out execution) || execution.CallerConnectionId != callerConnectionId)
                return;

            workerConnectionId = execution.WorkerConnectionId;
            if (workerConnectionId == null)
                _executions.Remove(executionId);
        }

        // A running execution is stopped by its worker, which completes it as canceled.
        if (workerConnectionId != null)
            await _workers.Clients.Client(workerConnectionId).Cancel(executionId);
        else
            await _callers.Clients.Client(callerConnectionId).Completed(execution.Request.Instance.Finished(EnumTaskState.Canceled));
    }

    /// <summary>
    /// Nobody is left to hear about the executions of [callerConnectionId] : they are dropped and the
    /// running ones stopped.
    /// </summary>
    public async Task RemoveCallerAsync(string callerConnectionId)
    {
        List<(Guid ExecutionId, string WorkerConnectionId)> running = [];
        lock (_lock)
        {
            var executions = _executions.Where(x => x.Value.CallerConnectionId == callerConnectionId).ToList();
            foreach (var (executionId, execution) in executions)
            {
                _executions.Remove(executionId);
                if (execution.WorkerConnectionId != null)
                    running.Add((executionId, execution.WorkerConnectionId));
            }
        }

        var cancels = running.Select(x => _workers.Clients.Client(x.WorkerConnectionId).Cancel(x.ExecutionId));
        await Task.WhenAll(cancels);
    }

    #endregion

    #region Workers

    public Task AddWorkerAsync(string connectionId, string workerId, int capacity)
    {
        lock (_lock)
            _workersByConnection[connectionId] = new Worker { ConnectionId = connectionId, Id = workerId, Capacity = capacity };
        return DispatchAsync();
    }

    /// <summary>
    /// The executions [connectionId] was running are failed : nothing will report on them anymore.
    /// </summary>
    public async Task RemoveWorkerAsync(string connectionId)
    {
        List<(string CallerConnectionId, RemoteInstance Instance)> lost = [];
        lock (_lock)
        {
            if (!_workersByConnection.Remove(connectionId, out var worker))
                return;

            foreach (Guid executionId in worker.Running)
            {
                if (!_executions.Remove(executionId, out var execution))
                    continue;
                RemoteInstance failed = execution.Request.Instance.Finished(EnumTaskState.Failed, $"The worker '{worker.Id}' running the task was lost.");
                lost.Add((execution.CallerConnectionId, failed));
            }
        }

        var completions = lost.Select(x => _callers.Clients.Client(x.CallerConnectionId).Completed(x.Instance));
        await Task.WhenAll(completions);
    }

    public Task ReportStateAsync(Guid executionId, RemoteInstance instance)
    {
        string? callerConnectionId = CallerOf(executionId);
        if (callerConnectionId == null)
            return Task.CompletedTask;
        return _callers.Clients.Client(callerConnectionId).StateChanged(executionId, instance);
    }

    public Task ReportNotificationAsync(Guid executionId, TaskNotification notification)
    {
        string? callerConnectionId = CallerOf(executionId);
        if (callerConnectionId == null)
            return Task.CompletedTask;
        return _callers.Clients.Client(callerConnectionId).Notified(executionId, notification);
    }

    public async Task CompleteAsync(string workerConnectionId, RemoteInstance instance)
    {
        Execution? execution;
        lock (_lock)
        {
            if (_workersByConnection.TryGetValue(workerConnectionId, out var worker))
                worker.Running.Remove(instance.Id);
            _executions.Remove(instance.Id, out execution);
        }

        // Unknown when its caller left in the meantime.
        if (execution != null)
            await _callers.Clients.Client(execution.CallerConnectionId).Completed(instance);

        await DispatchAsync();
    }

    #endregion

    private string? CallerOf(Guid executionId)
    {
        lock (_lock)
            return _executions.GetValueOrDefault(executionId)?.CallerConnectionId;
    }

    /// <summary>
    /// Hand the waiting executions to the workers having room for them.
    /// </summary>
    private async Task DispatchAsync()
    {
        List<(string WorkerConnectionId, RemoteExecution Request)> assigned = [];
        lock (_lock)
        {
            while (_queue.Count > 0)
            {
                // The least busy one, so the load is spread rather than piled on the first worker.
                Worker? worker = _workersByConnection.Values
                    .Where(x => x.Running.Count < x.Capacity)
                    .MinBy(x => x.Running.Count);
                if (worker == null)
                    break;

                Guid executionId = _queue.Dequeue();
                if (!_executions.TryGetValue(executionId, out var execution))
                    continue;

                execution.WorkerConnectionId = worker.ConnectionId;
                worker.Running.Add(executionId);
                assigned.Add((worker.ConnectionId, execution.Request));
            }
        }

        var sends = assigned.Select(x => _workers.Clients.Client(x.WorkerConnectionId).Execute(x.Request));
        await Task.WhenAll(sends);
    }
}
