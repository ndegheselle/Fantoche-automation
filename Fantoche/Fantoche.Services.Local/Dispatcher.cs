using Fantoche.Plugins.Shared;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Scoped;
using Fantoche.Shared.Remote;
using Fantoche.Shared.Services;
using Newtonsoft.Json.Linq;

namespace Fantoche.Services.Local;

/// <summary>
/// Hand the executions of the callers to the workers : the task is read from the database, the
/// states the workers report are stored in the history, and the outcome is sent back to the caller.
/// The executions themselves are kept in memory : one only lives as long as the connections of its
/// caller and its worker. A run whose worker is lost is interrupted, and can be resumed on any
/// worker from what the history holds of it (see <see cref="ResumeAsync"/>).
/// <para>
/// A caller and a worker are known by the id of their connection, whatever carries their messages
/// (a hub of the supervisor API, or a call in the same process).
/// </para>
/// </summary>
public class Dispatcher : IDispatchService
{
    private sealed class Worker
    {
        public required string ConnectionId { get; init; }
        public required IWorkerClient Client { get; init; }
        public required string Id { get; init; }
        public required int Capacity { get; init; }
        public HashSet<Guid> Running { get; } = [];
    }

    private sealed class Execution
    {
        /// <summary>Null for a resumed run, which nobody waits on but the history.</summary>
        public string? CallerConnectionId { get; set; }
        public ICallerClient? Caller { get; set; }
        public required RemoteExecution Request { get; init; }
        /// <summary>Null while it waits for a worker.</summary>
        public Worker? Worker { get; set; }
    }

    private readonly LocalScopedService _scoped;
    private readonly LocalHistoryService _history;

    private readonly object _lock = new();
    private readonly Dictionary<string, Worker> _workersByConnection = [];
    private readonly Dictionary<Guid, Execution> _executions = [];
    /// <summary>Executions waiting for a worker, the canceled ones being skipped once dequeued.</summary>
    private readonly Queue<Guid> _queue = [];
    /// <summary>Taken to read and raise a status at once, so they are raised in the order they were read.</summary>
    private readonly object _notifyLock = new();

    public event Action<DispatchStatus>? StatusChanged;

    public Dispatcher(LocalScopedService scoped, LocalHistoryService history)
    {
        _scoped = scoped;
        _history = history;
    }

    public Task<DispatchStatus> GetStatusAsync() => Task.FromResult(GetStatus());

    public DispatchStatus GetStatus()
    {
        lock (_lock)
        {
            // A running execution whose caller left is dropped while its worker is still stopping it.
            List<WorkerStatus> workers = [.. _workersByConnection.Values.Select(x => new WorkerStatus(
                x.Id,
                x.Capacity,
                [.. x.Running.Where(_executions.ContainsKey).Select(id => StatusOf(_executions[id]))]))];

            // The canceled ones stay in the queue until dequeued, but are gone from the executions.
            List<ExecutionStatus> pending = [.. _queue
                .Where(_executions.ContainsKey)
                .Select(id => StatusOf(_executions[id]))];

            return new DispatchStatus(workers, pending);
        }
    }

    private void NotifyStatusChanged()
    {
        lock (_notifyLock)
        {
            DispatchStatus status = GetStatus();
            StatusChanged?.Invoke(status);
        }
    }

    private static ExecutionStatus StatusOf(Execution execution)
    {
        TaskInstance instance = execution.Request.Instance;
        return new ExecutionStatus(instance.Id, instance.TaskId, instance.CreatedAt);
    }

    #region Callers

    public async Task SubmitAsync(string callerConnectionId, ICallerClient caller, TaskInstance instance)
    {
        // Whatever the caller sent, the instance runs from now on, on the workers of the supervisor.
        instance.State = EnumTaskState.Progressing;
        instance.ApplicationId = null;
        RemoteExecution execution;
        try
        {
            BaseAutomationTask task = await _scoped.LoadRunnableAsync(instance.TaskId);
            instance.DefinitionHash = RunDefinition.HashOf(task);
            execution = RemoteExecution.From(task, instance);
        }
        catch (ExecutionException exception)
        {
            // Not stored : the instance would point at a task the database doesn't hold.
            instance.State = EnumTaskState.Failed;
            instance.Output = JToken.FromObject(exception.Message);
            await caller.Completed(instance);
            return;
        }

        // Stored before the worker reports anything : the instances of the nodes point at it.
        await _history.SaveAsync(instance);
        lock (_lock)
        {
            _executions[instance.Id] = new Execution { CallerConnectionId = callerConnectionId, Caller = caller, Request = execution };
            _queue.Enqueue(instance.Id);
        }
        await DispatchAsync();
    }

    /// <summary>
    /// Queue the interrupted run [runId] again : the worker it is handed to walks it again from
    /// what the history holds of it, running its interrupted nodes again.
    /// </summary>
    /// <exception cref="ExecutionException">The run can't be resumed, see <see cref="RunResumption"/>.</exception>
    public async Task ResumeAsync(Guid runId)
    {
        TaskInstance run = await _history.GetAsync(runId)
            ?? throw new ExecutionException($"Unknown execution '{runId}'.");
        BaseAutomationTask task = await _scoped.LoadRunnableAsync(run.TaskId);
        RunResumption.EnsureResumable(run, task, applicationId: null);
        IReadOnlyList<TaskInstance> stored = await _history.GetDescendantsAsync(run.Id);

        run.State = EnumTaskState.Progressing;
        Execution execution = new() { Request = RemoteExecution.From(task, run, stored) };
        lock (_lock)
        {
            // Resumed twice at once : both read it interrupted.
            if (!_executions.TryAdd(run.Id, execution))
                throw new ExecutionException("The run is already resumed.");
            _queue.Enqueue(run.Id);
        }

        await _history.SaveAsync(run);
        await DispatchAsync();
    }

    public async Task CancelAsync(string callerConnectionId, Guid executionId)
    {
        Execution? execution;
        Worker? worker;
        lock (_lock)
        {
            if (!_executions.TryGetValue(executionId, out execution) || execution.CallerConnectionId != callerConnectionId)
                return;

            worker = execution.Worker;
            if (worker == null)
                _executions.Remove(executionId);
        }

        // A running execution is stopped by its worker, which completes it as canceled.
        if (worker != null)
            await worker.Client.Cancel(executionId);
        else
        {
            NotifyStatusChanged();
            await FinishAsync(execution, EnumTaskState.Canceled);
        }
    }

    /// <summary>
    /// Nobody is left to hear about the executions of [callerConnectionId] : the waiting ones are
    /// canceled, the running ones stopped by their worker, which still reports them to the end.
    /// </summary>
    public async Task RemoveCallerAsync(string callerConnectionId)
    {
        List<Execution> pending = [];
        List<(Guid ExecutionId, Worker Worker)> running = [];
        lock (_lock)
        {
            var executions = _executions.Where(x => x.Value.CallerConnectionId == callerConnectionId).ToList();
            foreach (var (executionId, execution) in executions)
            {
                execution.CallerConnectionId = null;
                execution.Caller = null;
                if (execution.Worker != null)
                    running.Add((executionId, execution.Worker));
                else if (_executions.Remove(executionId))
                    pending.Add(execution);
            }
        }
        if (pending.Count > 0)
            NotifyStatusChanged();

        foreach (Execution execution in pending)
            await FinishAsync(execution, EnumTaskState.Canceled);
        var cancels = running.Select(x => x.Worker.Client.Cancel(x.ExecutionId));
        await Task.WhenAll(cancels);
    }

    #endregion

    #region Workers

    public Task AddWorkerAsync(string connectionId, IWorkerClient client, string workerId, int capacity)
    {
        lock (_lock)
            _workersByConnection[connectionId] = new Worker { ConnectionId = connectionId, Client = client, Id = workerId, Capacity = capacity };
        return DispatchAsync();
    }

    /// <summary>
    /// The executions [connectionId] was running are interrupted : nothing will report on them
    /// anymore, they wait for someone to resume them.
    /// </summary>
    public async Task RemoveWorkerAsync(string connectionId)
    {
        List<Execution> lost = [];
        lock (_lock)
        {
            if (!_workersByConnection.Remove(connectionId, out var worker))
                return;

            foreach (Guid executionId in worker.Running)
            {
                if (_executions.Remove(executionId, out var execution))
                    lost.Add(execution);
            }
        }
        NotifyStatusChanged();

        foreach (Execution execution in lost)
            await InterruptAsync(execution);
    }

    /// <summary>
    /// Stored whoever it is for : the history is pushed to every application. Dropped when it comes
    /// from a worker that isn't running the execution anymore : one that lost its connection and
    /// reconnected meanwhile reports on a run that was interrupted, and maybe resumed elsewhere.
    /// </summary>
    public Task ReportStateAsync(string workerConnectionId, Guid executionId, TaskInstance instance)
    {
        if (!IsRunBy(workerConnectionId, executionId))
            return Task.CompletedTask;
        return _history.SaveAsync(instance);
    }

    public Task ReportNotificationAsync(Guid executionId, TaskNotification notification)
    {
        ICallerClient? caller;
        lock (_lock)
            caller = _executions.GetValueOrDefault(executionId)?.Caller;

        if (caller == null)
            return Task.CompletedTask;
        return caller.Notified(executionId, notification);
    }

    /// <summary>Dropped as a state is, see <see cref="ReportStateAsync"/>.</summary>
    public async Task CompleteAsync(string workerConnectionId, TaskInstance instance)
    {
        Execution? execution;
        lock (_lock)
        {
            if (!IsRunBy(workerConnectionId, instance.Id))
                return;

            _workersByConnection[workerConnectionId].Running.Remove(instance.Id);
            _executions.Remove(instance.Id, out execution);
        }

        await _history.SaveAsync(instance);
        if (execution?.Caller != null)
            await execution.Caller.Completed(instance);

        await DispatchAsync();
    }

    private bool IsRunBy(string workerConnectionId, Guid executionId)
    {
        lock (_lock)
            return _executions.TryGetValue(executionId, out var execution) && execution.Worker?.ConnectionId == workerConnectionId;
    }

    #endregion

    /// <summary>
    /// End [execution] without a worker having run it to its end : stored, then sent to its caller.
    /// </summary>
    private async Task FinishAsync(Execution execution, EnumTaskState state)
    {
        TaskInstance instance = execution.Request.Instance;
        instance.State = state;

        await _history.SaveAsync(instance);
        if (execution.Caller != null)
            await execution.Caller.Completed(instance);
    }

    /// <summary>
    /// [execution] lost its worker : interrupted in the history along with the nodes it was
    /// running, and sent to its caller as such.
    /// </summary>
    private async Task InterruptAsync(Execution execution)
    {
        Guid runId = execution.Request.Instance.Id;
        await _history.InterruptRunAsync(runId);
        if (execution.Caller == null)
            return;

        TaskInstance interrupted = await _history.GetAsync(runId) ?? execution.Request.Instance;
        await execution.Caller.Completed(interrupted);
    }

    /// <summary>
    /// Hand the waiting executions to the workers having room for them.
    /// </summary>
    private async Task DispatchAsync()
    {
        List<(Worker Worker, RemoteExecution Request)> assigned = [];
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

                execution.Worker = worker;
                worker.Running.Add(executionId);
                assigned.Add((worker, execution.Request));
            }
        }

        // Also what was just queued, a worker that came or an execution that ended : every change
        // to the dispatch goes through here.
        NotifyStatusChanged();

        var sends = assigned.Select(x => x.Worker.Client.Execute(x.Request));
        await Task.WhenAll(sends);
    }
}
