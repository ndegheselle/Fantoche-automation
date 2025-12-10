using Automation.Dal;
using Automation.Dal.Repositories;
using Automation.Models.Work;
using Automation.Plugins.Shared;
using Automation.Shared.Data;
using Automation.Shared.Data.Task;
using Automation.Worker.Control;
using Newtonsoft.Json.Linq;

namespace Automation.Worker.Executor;

public class LocalWorkflowExecutor
{
    private readonly LocalTaskExecutor _executor;

    public LocalWorkflowExecutor(LocalTaskExecutor executor)
    {
        _executor = executor;
    }

    public async Task<TaskOutput> ExecuteAsync(
        BaseAutomationTask automationTask,
        TaskInput input,
        IProgress<TaskNotification>? notifications = null,
        CancellationToken? cancellation = null)
    {
        if (automationTask is not AutomationWorkflow workflow)
            throw new Exception("Task is not a workflow.");

        return await StartAsync(workflow, input, notifications, cancellation);
    }

    private async Task<TaskOutput> StartAsync(AutomationWorkflow workflow, TaskInput input,
        IProgress<TaskNotification>? notifications, CancellationToken? cancellation)
    {
        var tasks = new List<Task>();
        foreach (var start in workflow.Graph.GetStartNodes())
            tasks.Add(NextAsync(start, workflow, input, notifications, cancellation));

        await Task.WhenAll(tasks);
        return await EndAsync(workflow);
    }

    private async Task<TaskOutput> EndAsync(AutomationWorkflow workflow)
    {
    }

    private async Task NextAsync(BaseGraphTask task, AutomationWorkflow workflow, TaskInput input,
        IProgress<TaskNotification>? notifications, CancellationToken? cancellation)
    {
        var nextTasks = workflow.Graph.GetNextFrom(task);
        var tasks = new List<Task>();
    
        foreach (var next in nextTasks)
        {
            if (next.Settings.IsWaitingAllInputs)
                next.WaitedInputs.Add(task.Name, input.InputToken);
    
            if (workflow.Graph.CanExecute(next))
            {
                // Capture 'next' for the async closure
                var nextTask = next;
                tasks.Add(Task.Run(async () =>
                {
                    var output = await ExecuteNodeAsync(nextTask, workflow, input, notifications, cancellation);
                    var nextInput = new TaskInput { ContextToken = input.ContextToken, InputToken = output.OutputToken };
                    await NextAsync(nextTask, workflow, nextInput, notifications, cancellation);
                }));
            }
        }
    
        await Task.WhenAll(tasks);
    }

    private async Task<TaskOutput> ExecuteNodeAsync(BaseGraphTask task, AutomationWorkflow workflow, TaskInput input,
        IProgress<TaskNotification>? notifications, CancellationToken? cancellation)
    {
        var context = workflow.Graph.Execution.GetContextFor(task, workflowInstance.Data);
        if (task.Settings.IsWaitingAllInputs)
            task.WaitedInputs.Clear();

        if (!string.IsNullOrEmpty(task.InputJson))
            input.InputToken = ReferencesHandler.ReplaceReferences(task.InputJson, input.ContextToken).ReplacedSetting;

        var workflowContext = new WorkflowContext(workflow, workflowInstance);

        var executorTask =
            _executor.ExecuteAsync(subInstance, workflowContext, cancellation: _cancellationTokenSource.Token);
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