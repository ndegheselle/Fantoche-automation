using Automation.Models.Work;
using Automation.Plugins.Shared;
using Automation.Shared.Data;
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
        JToken? input,
        IProgress<TaskNotification>? notifications = null,
        CancellationToken? cancellation = null)
    {
        if (automationTask is not AutomationWorkflow workflow)
            throw new Exception("Task is not a workflow.");

        if (workflow.Graph.IsRefreshed == false)
            throw new Exception("The workflow graph should be refreshed before being executed.");
        
        return await StartAsync(workflow, input, cancellation);
    }

    private async Task<TaskOutput> StartAsync(AutomationWorkflow workflow, JToken? input, CancellationToken? cancellation)
    {
        WorkflowContext context = new WorkflowContext(workflow);
        var tasks = new List<Task>();
        foreach (var start in workflow.Graph.GetStartNodes())
            tasks.Add(NextAsync(start, context, input, cancellation));

        await Task.WhenAll(tasks);
        return await EndAsync(workflow);
    }

    private async Task<TaskOutput> EndAsync(AutomationWorkflow workflow)
    {
    }

    private async Task NextAsync(BaseGraphTask task, WorkflowContext context, JToken? previous, CancellationToken? cancellation)
    {
        var nextTasks = context.Workflow.Graph.GetNextFrom(task);
        var tasks = new List<Task>();
    
        foreach (var next in nextTasks)
        {
            if (next.Settings.IsWaitingAllInputs)
                next.WaitedInputs.Add(task.Name, previous);
    
            if (context.Workflow.Graph.CanExecute(next))
            {
                // Capture 'next' for the async closure
                var nextTask = next;
                tasks.Add(Task.Run(async () =>
                {
                    var output = await ExecuteNodeAsync(nextTask, context, previous, cancellation);
                    await NextAsync(nextTask, context, output.OutputToken, cancellation);
                }));
            }
        }
    
        await Task.WhenAll(tasks);
    }

    private async Task<TaskOutput> ExecuteNodeAsync(BaseGraphTask task, WorkflowContext context, JToken? previous, CancellationToken? cancellation)
    {
        var taskContext = context.Workflow.Graph.Execution.GetContextFor(task, previous, context.Shared);
        if (task.Settings.IsWaitingAllInputs)
            task.WaitedInputs.Clear();

        JToken? input = null;
        if (!string.IsNullOrEmpty(task.InputJson))
            input = ReferencesHandler.ReplaceReferences(task.InputJson, taskContext).ReplacedSetting;

        return await _executor.ExecuteAsync(task.AutomationTask ?? throw new Exception("Workflow tasks are not loaded."), input, context, null, cancellation);
    }
}