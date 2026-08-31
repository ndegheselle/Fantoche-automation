using System.Collections.Concurrent;
using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Graph;
using Automation.Shared.Data.Scoped;
using Automation.Shared.Services;
using Automation.Worker.Executor;
using Automation.Worker.Packages;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace Automation.Services.Local;

/// <summary>
/// Run the tasks and workflows in the process of the application, and store every instance they
/// produce in the local history.
/// </summary>
public class LocalExecutionService : IExecutionService, IDisposable
{
    private class RunningExecution
    {
        public required TaskInstance Instance { get; init; }
        public required CancellationTokenSource Cancellation { get; init; }
        public required TaskCompletionSource<TaskInstance> Completion { get; init; }
    }

    private readonly LocalScopedService _scopedService;
    private readonly LocalHistoryService _historyService;
    private readonly LocalDbContextFactory _dbContextFactory;

    private readonly WorkflowExecutor _workflowExecutor;
    private readonly NodeExecutor _nodeExecutor;

    private readonly ConcurrentDictionary<Guid, RunningExecution> _running = [];

    public LocalExecutionService(
        LocalScopedService scopedService,
        LocalHistoryService historyService,
        LocalDbContextFactory dbContextFactory,
        LocalPackageManagement packageManagement)
    {
        _scopedService = scopedService;
        _historyService = historyService;
        _dbContextFactory = dbContextFactory;

        _workflowExecutor = new WorkflowExecutor(packageManagement);
        _nodeExecutor = new NodeExecutor(packageManagement, _workflowExecutor);
    }

    public void Dispose()
    {
        _nodeExecutor.Dispose();
    }

    public async Task<TaskInstance> StartAsync(Guid taskId, JToken? parameters = null)
    {
        return await StartAsync(await LoadTaskAsync(taskId), parameters);
    }

    public async Task<TaskInstance> StartAsync(BaseAutomationTask task, JToken? parameters = null)
    {
        // The context of the scopes containing the element is read once, when the execution starts.
        JToken globalContext = await _scopedService.GetContextAsync(task.Id);

        TaskInstance instance;
        if (task is AutomationWorkflow workflow)
        {
            await RefreshGraphAsync(workflow);
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

        using var db = _dbContextFactory.CreateDbContext();
        return await db.TaskInstances.AsNoTracking().FirstOrDefaultAsync(x => x.Id == instanceId)
            ?? throw new ExecutionException($"Unknown execution '{instanceId}'.");
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
            instance = await _nodeExecutor.ExecuteAsync(task, instance, progress, running.Cancellation.Token);
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
    /// Load the task or workflow [taskId], ready to be executed.
    /// </summary>
    private async Task<BaseAutomationTask> LoadTaskAsync(Guid taskId)
    {
        using var db = _dbContextFactory.CreateDbContext();

        var element = await db.ScopedElements.AsNoTracking().FirstOrDefaultAsync(x => x.Id == taskId)
            ?? throw new ExecutionException($"Unknown element '{taskId}'.");

        if (element is not BaseAutomationTask task)
            throw new ExecutionException($"The element '{element.Metadata.Name}' is not a task or a workflow.");

        return task;
    }

    /// <summary>
    /// Load the tasks the nodes of [workflow] point to and refresh its graph, sub workflows included :
    /// what the executor needs to walk the graph.
    /// </summary>
    private async Task RefreshGraphAsync(AutomationWorkflow workflow, HashSet<Guid>? visited = null)
    {
        visited ??= [];
        if (!visited.Add(workflow.Id))
            return;

        var nodesIds = workflow.Graph.Nodes.OfType<BaseGraphTask>().Select(x => x.TaskId).Distinct().ToList();

        using var db = _dbContextFactory.CreateDbContext();
        var elements = await db.ScopedElements.AsNoTracking()
            .Where(x => nodesIds.Contains(x.Id))
            .ToListAsync();

        Dictionary<Guid, BaseAutomationTask> tasks = [];
        foreach (var element in elements)
        {
            if (element is BaseAutomationTask task)
                tasks[task.Id] = task;
        }

        // The controls aren't stored elements, the graph knows them on its own.
        var missing = nodesIds
            .Where(id => !tasks.ContainsKey(id))
            .Where(id => id != AutomationControl.StartTask.Id
                && id != AutomationControl.EndTask.Id
                && id != AutomationControl.ShareTask.Id
                && id != AutomationControl.JoinTask.Id)
            .ToList();
        if (missing.Count > 0)
            throw new ExecutionException($"The workflow '{workflow.Metadata.Name}' points at unknown tasks : {string.Join(", ", missing)}.");

        foreach (var child in tasks.Values.OfType<AutomationWorkflow>())
            await RefreshGraphAsync(child, visited);

        workflow.Graph.Refresh(tasks, force: true);
    }

    /// <summary>
    /// Stores every reported instance as it changes. The reports are handled on the thread of the
    /// executor rather than posted like a <see cref="Progress{T}"/> would : the states of a same
    /// instance have to reach the history in the order they happened.
    /// </summary>
    private class HistoryProgress : IProgress<TaskInstance>
    {
        private readonly LocalHistoryService _historyService;

        public HistoryProgress(LocalHistoryService historyService)
        {
            _historyService = historyService;
        }

        public void Report(TaskInstance value)
        {
            _historyService.SaveAsync(value).GetAwaiter().GetResult();
        }
    }
}
