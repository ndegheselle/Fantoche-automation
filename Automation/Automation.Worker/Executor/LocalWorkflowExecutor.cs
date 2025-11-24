using Automation.Dal;
using Automation.Dal.Repositories;
using Automation.Models.Work;
using Automation.Shared.Data;
using Automation.Shared.Data.Task;
using Newtonsoft.Json.Linq;

namespace Automation.Worker.Executor;

public class LocalWorkflowExecutor
{
    private CancellationToken? _cancellation;
    private readonly CancellationTokenSource _cancellationTokenSource;
    
    private readonly ScopesRepository _scopesRepo;

    private readonly LocalTaskExecutor _executor;
    private readonly AutomationWorkflow _workflow;

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
        await Start(workflowInstance);
        return workflowInstance;
    }

    private async Task Start(TaskInstance workflowInstance)
    {
        if (workflowInstance.Data == null)
            workflowInstance.Data = new TaskInstanceData();

        // Add global context
        workflowInstance.Data.GlobalToken = await _scopesRepo.GetContextFromTree(_workflow.ParentTree);

        foreach (var start in _workflow.Graph.GetStartNodes()) Next(start, workflowInstance);
    }

    private void End()
    {
        // Stop all tasks that haven't finished
        _cancellationTokenSource.Cancel();
    }

    private void Next(BaseGraphTask task, TaskInstance workflowInstance)
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
                _ = Execute(next, workflowInstance);
        }
    }

    private async Task Execute(BaseGraphTask task, TaskInstance workflowInstance)
    {
        var subInstance = new SubTaskInstance(workflowInstance.Id, task);

        var context = _workflow.Graph.Execution.GetContextFor(task, workflowInstance.Data);
        if (task.Settings.IsWaitingAllInputs)
            task.WaitedInputs.Clear();

        JToken? input = null;
        if (string.IsNullOrEmpty(task.InputJson) == false)
            input = ReferencesHandler.ReplaceReferences(JToken.Parse(task.InputJson), context).ReplacedSetting;

        await _executor.ExecuteAsync(subInstance, null, _cancellationTokenSource.Token);

        Next(task, workflowInstance);
    }
}