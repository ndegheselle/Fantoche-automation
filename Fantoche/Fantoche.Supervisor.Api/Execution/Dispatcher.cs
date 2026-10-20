using Fantoche.Plugins.Shared;
using Fantoche.Services.Local;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Scoped;
using Fantoche.Shared.Remote;
using Fantoche.Shared.Services;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json.Linq;

namespace Fantoche.Supervisor.Api.Execution;

/// <summary>
/// Hand the executions of the callers to the workers : the task is read from the database, the
/// states the workers report are stored in the history, and the outcome is sent back to the caller.
/// The executions themselves are kept in memory : one only lives as long as the connections of its
/// caller and its worker.
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

    private readonly LocalScopedService _scoped;
    private readonly LocalHistoryService _history;
    private readonly IHubContext<CallerHub, ICallerClient> _callers;
    private readonly IHubContext<WorkerHub, IWorkerClient> _workers;

    private readonly object _lock = new();
    private readonly Dictionary<string, Worker> _workersByConnection = [];
    private readonly Dictionary<Guid, Execution> _executions = [];
    /// <summary>Executions waiting for a worker, the canceled ones being skipped once dequeued.</summary>
    private readonly Queue<Guid> _queue = [];

    public Dispatcher(
        LocalScopedService scoped,
        LocalHistoryService history,
        IHubContext<CallerHub, ICallerClient> callers,
        IHubContext<WorkerHub, IWorkerClient> workers)
    {
        _scoped = scoped;
        _history = history;
        _callers = callers;
        _workers = workers;
    }

    public WorkersOverview GetOverview()
    {
        lock (_lock)
        {
            // Left out once dropped : a canceled execution stays queued until it is dequeued, and on
            // its worker until the worker reports it stopped.
            List<WorkerInfo> workers = [];
            foreach (Worker worker in _workersByConnection.Values.OrderBy(x => x.Id))
            {
                List<ExecutionSummary> running = [.. worker.Running
                    .Where(_executions.ContainsKey)
                    .Select(x => SummaryOf(_executions[x]))];
                workers.Add(new WorkerInfo(worker.Id, worker.Capacity, running));
            }

            List<ExecutionSummary> waiting = [.. _queue
                .Where(_executions.ContainsKey)
                .Select(x => SummaryOf(_executions[x]))];

            return new WorkersOverview(workers, waiting);
        }
    }

    private static ExecutionSummary SummaryOf(Execution execution)
    {
        TaskInstance instance = execution.Request.Instance;
        return new ExecutionSummary(instance.Id, instance.TaskId, execution.Request.Task.Metadata.Name, instance.CreatedAt);
    }

    #region Callers

    public async Task SubmitAsync(string callerConnectionId, TaskInstance instance, JToken? globalContext)
    {
        // Whatever the caller sent, the instance runs from now on.
        instance.State = EnumTaskState.Progressing;
        RemoteExecution execution;
        try
        {
            BaseAutomationTask task = await _scoped.LoadRunnableAsync(instance.TaskId);
            execution = RemoteExecution.From(task, instance, globalContext);
        }
        catch (ExecutionException exception)
        {
            // Not stored : the instance would point at a task the database doesn't hold.
            instance.State = EnumTaskState.Failed;
            instance.Output = JToken.FromObject(exception.Message);
            await _callers.Clients.Client(callerConnectionId).Completed(instance);
            return;
        }

        // Stored before the worker reports anything : the instances of the nodes point at it.
        await _history.SaveAsync(instance);
        lock (_lock)
        {
            _executions[instance.Id] = new Execution { CallerConnectionId = callerConnectionId, Request = execution };
            _queue.Enqueue(instance.Id);
        }
        await DispatchAsync();
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
            await FinishAsync(callerConnectionId, execution.Request.Instance, EnumTaskState.Canceled);
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
        string workerId;
        List<Execution> lost = [];
        lock (_lock)
        {
            if (!_workersByConnection.Remove(connectionId, out var worker))
                return;

            workerId = worker.Id;
            foreach (Guid executionId in worker.Running)
            {
                if (_executions.Remove(executionId, out var execution))
                    lost.Add(execution);
            }
        }

        foreach (Execution execution in lost)
            await FinishAsync(execution.CallerConnectionId, execution.Request.Instance, EnumTaskState.Failed, $"The worker '{workerId}' running the task was lost.");
    }

    /// <summary>
    /// Stored whoever it is for : the history is pushed to every application.
    /// </summary>
    public Task ReportStateAsync(TaskInstance instance) => _history.SaveAsync(instance);

    public Task ReportNotificationAsync(Guid executionId, TaskNotification notification)
    {
        string? callerConnectionId;
        lock (_lock)
            callerConnectionId = _executions.GetValueOrDefault(executionId)?.CallerConnectionId;

        if (callerConnectionId == null)
            return Task.CompletedTask;
        return _callers.Clients.Client(callerConnectionId).Notified(executionId, notification);
    }

    public async Task CompleteAsync(string workerConnectionId, TaskInstance instance)
    {
        await _history.SaveAsync(instance);

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

    /// <summary>
    /// End [instance] without a worker having run it to its end : stored, then sent to its caller.
    /// </summary>
    private async Task FinishAsync(string callerConnectionId, TaskInstance instance, EnumTaskState state, string? error = null)
    {
        instance.State = state;
        if (error != null)
            instance.Output = JToken.FromObject(error);

        await _history.SaveAsync(instance);
        await _callers.Clients.Client(callerConnectionId).Completed(instance);
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
