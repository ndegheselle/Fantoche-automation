using System.Collections.Concurrent;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Scoped;
using Fantoche.Shared.Services;
using Newtonsoft.Json.Linq;

namespace Fantoche.Worker.Executor;

/// <summary>
/// Run the tasks and workflows with the given executor (in the application or on a worker), and
/// store every instance they produce in the history, wherever the services keep it. The runs are
/// marked as the ones of this application, which alone resumes them once suspended.
/// </summary>
public class ExecutionService : IExecutionService
{
    private class RunningExecution
    {
        public required TaskInstance Instance { get; init; }
        public required CancellationTokenSource Cancellation { get; init; }
        /// <summary>See <see cref="WorkflowInstance.PauseRequested"/>.</summary>
        public required CancellationTokenSource Pause { get; init; }
        public required TaskCompletionSource<TaskInstance> Completion { get; init; }
    }

    private readonly IScopedService _scopedService;
    private readonly IHistoryService _historyService;

    private readonly ITaskExecutor _executor;
    private readonly string _applicationId;
    private readonly RunnerSessions _sessions;

    private readonly ConcurrentDictionary<Guid, RunningExecution> _running = [];

    public event Action? RunningChanged;

    /// <param name="applicationId">
    /// What the runs of this application are known by (see <see cref="TaskInstance.ApplicationId"/>) :
    /// its machine name, which stays the same from one start to the next. The copies of the
    /// application running on the machine share it, any of them resumes the runs of the others.
    /// </param>
    /// <param name="sessions">The copies of the application on this machine, this one included.</param>
    public ExecutionService(
        IScopedService scopedService,
        IHistoryService historyService,
        ITaskExecutor executor,
        string applicationId,
        RunnerSessions sessions)
    {
        _scopedService = scopedService;
        _historyService = historyService;
        _executor = executor;
        _applicationId = applicationId;
        _sessions = sessions;
    }

    /// <summary>
    /// Mark the runs a copy of this application left behind as it stopped as suspended, so they
    /// can be resumed. The ones a running copy runs are left alone.
    /// </summary>
    public Task SuspendLeftRunsAsync() => _sessions.SuspendLeftRunsAsync(_historyService, _applicationId);

    public async Task<TaskInstance> StartAsync(Guid taskId, JToken? parameters = null)
    {
        BaseAutomationTask task = await _scopedService.LoadRunnableAsync(taskId);

        // The context of the scopes containing the element is read once, when the execution starts.
        JToken globalContext = await _scopedService.GetContextAsync(task.Id);

        TaskInstance instance;
        if (task is AutomationWorkflow workflow)
        {
            instance = new WorkflowInstance(workflow)
            {
                Parameters = parameters,
                GlobalContext = globalContext,
            };
        }
        else
        {
            instance = new TaskInstance() { TaskId = task.Id, Parameters = parameters };
        }

        instance.NodeName = task.Metadata.Name;
        instance.ApplicationId = _applicationId;
        instance.SessionId = _sessions.CurrentId;
        instance.NestedVersions = task is AutomationWorkflow started ? ScopedServiceExtensions.NestedVersionsOf(started) : null;
        instance.State = EnumTaskState.Progressing;
        await _historyService.SaveAsync(instance);

        Launch(task, instance);
        return instance;
    }

    public async Task<TaskInstance> ResumeAsync(Guid runId)
    {
        TaskInstance run = await _historyService.GetAsync(runId)
            ?? throw new Shared.Services.ExecutionException($"Unknown execution '{runId}'.");
        RunResumption.EnsureResumable(run, _applicationId);
        BaseAutomationTask task = await _scopedService.LoadRunnableAsync(run.TaskId, run.WorkflowVersion, run.NestedVersions);

        // A task simply runs again, a workflow carries on from what its history holds.
        TaskInstance instance = run;
        if (task is AutomationWorkflow workflow)
        {
            WorkflowInstance resumed = WorkflowInstance.From(workflow, run);
            resumed.StoredInstances = await _historyService.GetDescendantsAsync(run.Id);
            instance = resumed;
        }

        // Resumed twice at once, by this copy of the application or another one.
        if (!await _historyService.ClaimRunAsync(instance.Id, _sessions.CurrentId))
            throw new Shared.Services.ExecutionException("The run is already resumed.");
        instance.State = EnumTaskState.Progressing;
        instance.SessionId = _sessions.CurrentId;

        Launch(task, instance);
        return instance;
    }

    /// <summary>
    /// Run [instance] apart : the caller follows it through the history rather than waiting on it.
    /// </summary>
    /// <exception cref="Shared.Services.ExecutionException">[instance] already runs : a run resumed twice at once.</exception>
    private void Launch(BaseAutomationTask task, TaskInstance instance)
    {
        var running = new RunningExecution()
        {
            Instance = instance,
            Cancellation = new CancellationTokenSource(),
            Pause = new CancellationTokenSource(),
            Completion = new TaskCompletionSource<TaskInstance>(TaskCreationOptions.RunContinuationsAsynchronously),
        };
        if (!_running.TryAdd(instance.Id, running))
            throw new Shared.Services.ExecutionException("The run is already resumed.");
        if (instance is WorkflowInstance workflow)
            workflow.PauseRequested = running.Pause.Token;
        RunningChanged?.Invoke();

        _ = Task.Run(() => RunAsync(task, running));
    }

    public async Task<TaskInstance> WaitAsync(Guid instanceId)
    {
        if (_running.TryGetValue(instanceId, out var running))
            return await running.Completion.Task;

        // Nothing is running under that id anymore : what the history holds of it is its outcome.
        return await _historyService.GetAsync(instanceId)
            ?? throw new Shared.Services.ExecutionException($"Unknown execution '{instanceId}'.");
    }

    public async Task CancelAsync(Guid instanceId)
    {
        if (_running.TryGetValue(instanceId, out var running))
        {
            running.Cancellation.Cancel();
            return;
        }

        TaskInstance? run = await _historyService.GetAsync(instanceId);
        if (run == null)
            return;

        // A suspended run only waits to be resumed : stopping it is giving up on it.
        if (run.State == EnumTaskState.Suspended && run.ApplicationId == _applicationId)
        {
            run.State = EnumTaskState.Canceled;
            await _historyService.SaveAsync(run);
            return;
        }

        if ((run.State & EnumTaskState.Finished) == 0)
            throw new Shared.Services.ExecutionException("The run is run by another application : only it can stop it.");
    }

    public Task PauseAsync(Guid instanceId)
    {
        if (!_running.TryGetValue(instanceId, out var running))
            throw new Shared.Services.ExecutionException("The run isn't run by this application : only the one running it can pause it.");

        running.Pause.Cancel();
        return Task.CompletedTask;
    }

    public IReadOnlyCollection<TaskInstance> GetRunning()
    {
        return _running.Values
            .Select(x => x.Instance)
            .OrderByDescending(x => x.CreatedAt)
            .ToList();
    }

    private async Task RunAsync(BaseAutomationTask task, RunningExecution running)
    {
        TaskInstance instance = running.Instance;
        var progress = new TaskInstancesProgress()
        {
            StateChanges = new HistoryProgress(_historyService),
        };

        try
        {
            instance = await _executor.ExecuteAsync(task, instance, progress, running.Cancellation.Token);
        }
        catch (Exception ex)
        {
            // The node executor swallows the failures of the task itself, anything reaching here
            // comes from the execution setup and still has to be reported as a failed instance.
            instance.State = EnumTaskState.Failed;
            instance.Output = JToken.FromObject(ex.ToString());
        }
        finally
        {
            await _historyService.SaveAsync(instance);
            _running.TryRemove(running.Instance.Id, out _);
            running.Cancellation.Dispose();
            running.Pause.Dispose();
            running.Completion.TrySetResult(instance);
            RunningChanged?.Invoke();
        }
    }

    /// <summary>
    /// Stores every reported instance as it changes. The reports are handled on the thread of the
    /// executor rather than posted like a <see cref="Progress{T}"/> would : the states of a same
    /// instance have to reach the history in the order they happened.
    /// </summary>
    private class HistoryProgress : IProgress<TaskInstance>
    {
        private readonly IHistoryService _historyService;

        public HistoryProgress(IHistoryService historyService)
        {
            _historyService = historyService;
        }

        public void Report(TaskInstance value)
        {
            _historyService.SaveAsync(value).GetAwaiter().GetResult();
        }
    }
}
