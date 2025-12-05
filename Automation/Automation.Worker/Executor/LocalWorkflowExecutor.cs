using Automation.Dal;
using Automation.Dal.Repositories;
using Automation.Models.Work;
using Automation.Shared.Data;
using Automation.Shared.Data.Task;
using Automation.Worker.Control;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Automation.Worker.Executor;

public class LocalWorkflowExecutor
{
    private CancellationToken? _cancellation;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly List<Task> _runningTasks = new();

    private readonly ScopesRepository _scopesRepo;
    private readonly LocalTaskExecutor _executor;
    private readonly AutomationWorkflow _workflow;

    private readonly object _lock = new();

    public LocalWorkflowExecutor(DatabaseConnection connection, LocalTaskExecutor executor, AutomationWorkflow workflow)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _workflow = workflow;
        _scopesRepo = new ScopesRepository(connection);
        _executor = executor;
    }

    public async Task<TaskInstance> ExecuteAsync(
        TaskInstance workflowInstance,
        IProgress<TaskInstanceNotification>? progress = null,
        CancellationToken? cancellation = null)
    {
        _cancellation = cancellation;
        await StartAsync(workflowInstance);
        return workflowInstance;
    }


    public async Task CancelAsync()
    {
        _cancellationTokenSource.Cancel();

        try
        {
            await Task.WhenAll(_runningTasks);
        }
        catch (OperationCanceledException) { }
        finally
        {
            _runningTasks.Clear();
        }
    }

    private async Task StartAsync(TaskInstance workflowInstance)
    {
        workflowInstance.Data ??= new TaskInstanceData();
        workflowInstance.Data.GlobalToken = await _scopesRepo.GetContextFromTree(_workflow.ParentTree);

        foreach (var start in _workflow.Graph.GetStartNodes())
        {
            await NextAsync(start, workflowInstance);
        }
    }

    private async Task NextAsync(BaseGraphTask task, TaskInstance workflowInstance)
    {
        if (_cancellation?.IsCancellationRequested == true)
        {
            workflowInstance.State = EnumTaskState.Canceled;
            _cancellationTokenSource.Cancel();
            return;
        }

        var nextTasks = _workflow.Graph.GetNextFrom(task);
        foreach (var next in nextTasks)
        {
            if (next.Settings.IsWaitingAllInputs)
                next.WaitedInputs.Add(task.Name, workflowInstance.Data?.InputToken);

            if (_workflow.Graph.CanExecute(next))
            {
                await ExecuteAsync(next, workflowInstance);
                await NextAsync(next, workflowInstance);
            }
        }
    }

    private async Task ExecuteAsync(BaseGraphTask task, TaskInstance workflowInstance)
    {
        var subInstance = new SubTaskInstance(workflowInstance.Id, task);

        var context = _workflow.Graph.Execution.GetContextFor(task, workflowInstance.Data);
        if (task.Settings.IsWaitingAllInputs)
            task.WaitedInputs.Clear();

        JToken? input = null;
        if (!string.IsNullOrEmpty(task.InputJson))
            input = ReferencesHandler.ReplaceReferences(JToken.Parse(task.InputJson), context).ReplacedSetting;

        WorkflowContext workflowContext = new WorkflowContext(_workflow);

        var executorTask = _executor.ExecuteAsync(subInstance, workflowContext, null, _cancellationTokenSource.Token);
        Track(executorTask);
    }

    /// <summary>
    /// Track the running tasks.
    /// </summary>
    /// <param name="task"></param>
    private void Track(Task task)
    {
        lock (_lock)
        {
            _runningTasks.Add(task);
        }

        task.ContinueWith(t =>
        {
            lock (_lock)
            {
                _runningTasks.Remove(t);
            }
        }, TaskContinuationOptions.ExecuteSynchronously);
    }
}