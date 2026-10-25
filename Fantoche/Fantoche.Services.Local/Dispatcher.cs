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
/// caller and its worker. A run whose worker is lost is suspended, and can be resumed on any
/// worker from what the history holds of it (see <see cref="ResumeAsync"/>).
/// <para>
/// A caller and a worker are known by the id of their connection, whatever carries their messages
/// (a hub of the supervisor API, or a call in the same process).
/// </para>
/// <para>
/// A task asking the user (see <see cref="UserInterfaceSwitch"/>) is handed to an application
/// rather than to a worker : the worker running its run sends it as any caller does, and the
/// application runs it as any worker does, the run's storage being reached through the supervisor.
/// It waits in the queue, waiting for the user in the history, until the user of an application
/// takes it (see <see cref="TakeAsync"/>) : no page shows up unasked.
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
        public required EnumWorkerRoles Roles { get; init; }
        public HashSet<Guid> Running { get; } = [];

        /// <summary>How many runs it runs : the tasks asking the user wait on the user, not on its room.</summary>
        public int RunsCount(Dictionary<Guid, Execution> executions)
            => Running.Count(id => executions.TryGetValue(id, out Execution? execution) && !execution.AsksUser);
    }

    private sealed class Execution
    {
        /// <summary>Null for a resumed or scheduled run, which nobody waits on but the history.</summary>
        public string? CallerConnectionId { get; set; }
        public ICallerClient? Caller { get; set; }
        public required RemoteExecution Request { get; init; }
        /// <summary>Null while it waits for a worker.</summary>
        public Worker? Worker { get; set; }

        /// <summary>Whether it asks the user, handed to an application rather than to a worker.</summary>
        public bool AsksUser { get; init; }
    }

    private readonly LocalScopedService _scoped;
    private readonly LocalHistoryService _history;
    /// <summary>What the runs it dispatches are marked with, see <see cref="TaskInstance.SessionId"/>.</summary>
    private readonly string _sessionId;

    private readonly object _lock = new();
    private readonly Dictionary<string, Worker> _workersByConnection = [];
    private readonly Dictionary<Guid, Execution> _executions = [];
    /// <summary>Executions waiting for a worker, the canceled ones being skipped once dequeued.</summary>
    private readonly Queue<Guid> _queue = [];
    /// <summary>Taken to read and raise a status at once, so they are raised in the order they were read.</summary>
    private readonly object _notifyLock = new();

    public event Action<DispatchStatus>? StatusChanged;

    public Dispatcher(LocalScopedService scoped, LocalHistoryService history, string sessionId)
    {
        _scoped = scoped;
        _history = history;
        _sessionId = sessionId;
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
                x.Roles,
                [.. x.Running.Where(_executions.ContainsKey).Select(id => StatusOf(_executions[id]))]))];

            // The canceled ones stay in the queue until dequeued, but are gone from the executions,
            // and the ones taken by a user are already handed out.
            List<ExecutionStatus> pending = [.. _queue
                .Where(id => _executions.TryGetValue(id, out Execution? execution) && execution.Worker == null)
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
        BaseAutomationTask task;
        try
        {
            task = await _scoped.LoadRunnableAsync(instance.TaskId);
        }
        catch (ExecutionException exception)
        {
            // Not stored : the instance would point at a task the database doesn't hold.
            instance.State = EnumTaskState.Failed;
            instance.Output = JToken.FromObject(exception.Message);
            await caller.Completed(instance);
            return;
        }

        await QueueAsync(task, instance, callerConnectionId, caller);
    }

    /// <summary>
    /// Start [taskId], named [name], with [parameters] : what a schedule starts, which nobody waits on
    /// but the history. One that can't be loaded is stored as failed, the history being the only
    /// place telling it.
    /// </summary>
    public async Task<TaskInstance> StartAsync(Guid taskId, string name, JToken? parameters)
    {
        TaskInstance instance = new() { TaskId = taskId, NodeName = name, Parameters = parameters };
        BaseAutomationTask task;
        try
        {
            task = await _scoped.LoadRunnableAsync(taskId);
        }
        catch (ExecutionException exception)
        {
            instance.State = EnumTaskState.Failed;
            instance.Output = JToken.FromObject(exception.Message);
            await _history.SaveAsync(instance);
            return instance;
        }

        // The context of the scopes containing the workflow is read once, when the run starts.
        if (task is AutomationWorkflow)
            instance.GlobalContext = await _scoped.GetContextAsync(taskId);

        await QueueAsync(task, instance, callerConnectionId: null, caller: null);
        return instance;
    }

    /// <summary>
    /// Queue [instance], an execution of [task], for the workers. [caller] is told the outcome, when
    /// someone waits on it.
    /// </summary>
    private async Task QueueAsync(BaseAutomationTask task, TaskInstance instance, string? callerConnectionId, ICallerClient? caller)
    {
        // Whatever the caller sent, the instance runs from now on, on the workers of the supervisor.
        // One asking the user waits for one to take it.
        bool asksUser = UserInterfaceSwitch.IsOn(task, instance.Parameters);
        instance.State = asksUser ? EnumTaskState.AwaitingUser : EnumTaskState.Progressing;
        instance.ApplicationId = null;
        instance.SessionId = _sessionId;
        AutomationWorkflow? workflow = task as AutomationWorkflow;
        instance.WorkflowVersion = workflow?.Version;
        instance.NestedVersions = workflow == null ? null : ScopedServiceExtensions.NestedVersionsOf(workflow);

        // A node handed over by the worker running its run : the application uses the storage of that run.
        Guid? runId = instance.ParentInstanceId == null ? null : (await RunOfAsync(instance)).Id;
        RemoteExecution execution = RemoteExecution.From(task, instance, runId: runId);

        // Stored before the worker reports anything : the instances of the nodes point at it.
        await _history.SaveAsync(instance);
        lock (_lock)
        {
            _executions[instance.Id] = new Execution { CallerConnectionId = callerConnectionId, Caller = caller, Request = execution, AsksUser = asksUser };
            _queue.Enqueue(instance.Id);
        }
        await DispatchAsync();
    }

    /// <summary>The run [instance] belongs to : the instance at the top of its parents.</summary>
    private async Task<TaskInstance> RunOfAsync(TaskInstance instance)
    {
        TaskInstance run = instance;
        while (run.ParentInstanceId is Guid parentId)
        {
            run = await _history.GetAsync(parentId)
                ?? throw new ExecutionException($"The run of '{instance.NodeName}' isn't in the history.");
        }
        return run;
    }

    /// <summary>
    /// Queue the suspended run [runId] again : the worker it is handed to walks it again
    /// from what the history holds of it, running the nodes it was running again.
    /// </summary>
    /// <exception cref="ExecutionException">The run can't be resumed, see <see cref="RunResumption"/>.</exception>
    public async Task ResumeAsync(Guid runId)
    {
        TaskInstance run = await _history.GetAsync(runId)
            ?? throw new ExecutionException($"Unknown execution '{runId}'.");
        RunResumption.EnsureResumable(run, applicationId: null);
        BaseAutomationTask task = await _scoped.LoadRunnableAsync(run.TaskId, run.WorkflowVersion, run.NestedVersions);
        IReadOnlyList<TaskInstance> stored = await _history.GetDescendantsAsync(run.Id);

        // Resumed twice at once, by this supervisor or another one : both read it suspended.
        if (!await _history.ClaimRunAsync(run.Id, _sessionId))
            throw new ExecutionException("The run is already resumed.");

        run.State = EnumTaskState.Progressing;
        run.SessionId = _sessionId;
        Execution execution = new() { Request = RemoteExecution.From(task, run, stored) };
        lock (_lock)
        {
            _executions[run.Id] = execution;
            _queue.Enqueue(run.Id);
        }

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

    public Task AddWorkerAsync(string connectionId, IWorkerClient client, string workerId, int capacity, EnumWorkerRoles roles)
    {
        lock (_lock)
            _workersByConnection[connectionId] = new Worker { ConnectionId = connectionId, Client = client, Id = workerId, Capacity = capacity, Roles = roles };
        return DispatchAsync();
    }

    /// <summary>
    /// Hand [executionId], a task asking the user waiting in the queue, to the application of
    /// [connectionId], its user taking it. False when it doesn't wait anymore : taken by another
    /// application, canceled or over.
    /// </summary>
    public async Task<bool> TakeAsync(string connectionId, Guid executionId)
    {
        Worker? worker;
        RemoteExecution request;
        lock (_lock)
        {
            if (!_workersByConnection.TryGetValue(connectionId, out worker) || !worker.Roles.HasFlag(EnumWorkerRoles.UserTasks))
                throw new ExecutionException("Only an application takes a task asking the user.");
            if (!_executions.TryGetValue(executionId, out Execution? execution) || !execution.AsksUser || execution.Worker != null)
                return false;

            // Left in the queue : it is dropped from there once dequeued.
            execution.Worker = worker;
            worker.Running.Add(executionId);
            request = execution.Request;
        }

        NotifyStatusChanged();
        await worker.Client.Execute(request);
        return true;
    }

    /// <summary>
    /// The executions [connectionId] was running are suspended : nothing will report on them
    /// anymore, they wait for someone to resume them.
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
        NotifyStatusChanged();

        foreach (Execution execution in lost)
            await SuspendAsync(execution, $"The worker '{workerId}' running it was lost.");
    }

    /// <summary>
    /// Stored whoever it is for : the history is pushed to every application. Dropped when it comes
    /// from a worker that isn't running the execution anymore : one that lost its connection and
    /// reconnected meanwhile reports on a run that was suspended, and maybe resumed elsewhere.
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

    /// <summary>
    /// The least busy worker a run can be handed to, so the load is spread rather than piled on the
    /// first one. Called under the lock.
    /// </summary>
    private Worker? WorkerForRun()
    {
        return _workersByConnection.Values
            .Where(x => x.Roles.HasFlag(EnumWorkerRoles.Runs) && x.RunsCount(_executions) < x.Capacity)
            .MinBy(x => x.RunsCount(_executions));
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
    /// [execution] lost its worker : suspended in the history along with the nodes it was running,
    /// and sent to its caller as such.
    /// </summary>
    private async Task SuspendAsync(Execution execution, string reason)
    {
        Guid runId = execution.Request.Instance.Id;
        await _history.SuspendRunAsync(runId, reason);
        if (execution.Caller == null)
            return;

        TaskInstance suspended = await _history.GetAsync(runId) ?? execution.Request.Instance;
        await execution.Caller.Completed(suspended);
    }

    /// <summary>
    /// Hand the waiting runs to the workers having room for them, in order. The tasks asking the
    /// user wait on, for a user to take them (see <see cref="TakeAsync"/>) : they don't hold the runs
    /// queued after them.
    /// </summary>
    private async Task DispatchAsync()
    {
        List<(Worker Worker, RemoteExecution Request)> assigned = [];
        lock (_lock)
        {
            List<Guid> waiting = [];
            while (_queue.TryDequeue(out Guid executionId))
            {
                // Canceled, or taken by the user of an application.
                if (!_executions.TryGetValue(executionId, out var execution) || execution.Worker != null)
                    continue;

                Worker? worker = execution.AsksUser ? null : WorkerForRun();
                if (worker == null)
                {
                    waiting.Add(executionId);
                    continue;
                }

                execution.Worker = worker;
                worker.Running.Add(executionId);
                assigned.Add((worker, execution.Request));
            }

            foreach (Guid executionId in waiting)
                _queue.Enqueue(executionId);
        }

        // Also what was just queued, a worker that came or an execution that ended : every change
        // to the dispatch goes through here.
        NotifyStatusChanged();

        var sends = assigned.Select(x => x.Worker.Client.Execute(x.Request));
        await Task.WhenAll(sends);
    }
}
