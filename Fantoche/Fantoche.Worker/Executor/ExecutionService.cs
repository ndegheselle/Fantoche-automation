using System.Collections.Concurrent;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Scoped;
using Fantoche.Shared.Services;
using Newtonsoft.Json.Linq;

namespace Fantoche.Worker.Executor;

/// <summary>
/// Run the tasks and workflows with the given executor (in the application or on a worker), and
/// store every instance they produce in the history, wherever the services keep it.
/// </summary>
public class ExecutionService : IExecutionService
{
    private class RunningExecution
    {
        public required TaskInstance Instance { get; init; }
        public required CancellationTokenSource Cancellation { get; init; }
        public required TaskCompletionSource<TaskInstance> Completion { get; init; }
    }

    private readonly IScopedService _scopedService;
    private readonly IHistoryService _historyService;

    private readonly ITaskExecutor _executor;

    private readonly ConcurrentDictionary<Guid, RunningExecution> _running = [];

    public ExecutionService(
        IScopedService scopedService,
        IHistoryService historyService,
        ITaskExecutor executor)
    {
        _scopedService = scopedService;
        _historyService = historyService;
        _executor = executor;
    }

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
        instance.State = EnumTaskState.Progressing;
        await _historyService.SaveAsync(instance);

        var running = new RunningExecution()
        {
            Instance = instance,
            Cancellation = new CancellationTokenSource(),
            Completion = new TaskCompletionSource<TaskInstance>(TaskCreationOptions.RunContinuationsAsynchronously),
        };
        _running[instance.Id] = running;

        // Starting doesn't wait for the end of the run : the caller follows it through the history.
        _ = Task.Run(() => RunAsync(task, running));

        return instance;
    }

    public async Task<TaskInstance> WaitAsync(Guid instanceId)
    {
        if (_running.TryGetValue(instanceId, out var running))
            return await running.Completion.Task;

        // Nothing is running under that id anymore : what the history holds of it is its outcome.
        return await _historyService.GetAsync(instanceId)
            ?? throw new Shared.Services.ExecutionException($"Unknown execution '{instanceId}'.");
    }

    public Task CancelAsync(Guid instanceId)
    {
        if (_running.TryGetValue(instanceId, out var running))
            running.Cancellation.Cancel();
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
            running.Completion.TrySetResult(instance);
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
