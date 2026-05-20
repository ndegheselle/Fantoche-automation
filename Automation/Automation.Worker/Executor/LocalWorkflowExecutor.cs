using Automation.Models.Work;
using Automation.Plugins.Shared;
using Automation.Shared.Data;
using Automation.Shared.Data.Task;
using Automation.Worker.Control;
using Newtonsoft.Json.Linq;
using System.Collections;
using static System.Net.Mime.MediaTypeNames;

namespace Automation.Worker.Executor;

public class LocalWorkflowExecutor
{
    private readonly LocalTaskExecutor _executor;
    private readonly ITaskChangeHandler? _changes;

    public LocalWorkflowExecutor(LocalTaskExecutor executor, ITaskChangeHandler? changes)
    {
        _executor = executor;
        _changes = changes;
    }

    public async Task<TaskOutput> ExecuteAsync(
        AutomationWorkflow workflow,
        JToken? input,
        IProgress<TaskNotification>? notifications = null,
        CancellationToken? cancellation = null)
    {
        if (workflow.Graph.IsRefreshed == false)
            throw new Exception("The workflow graph should be refreshed before being executed.");
        
        return await StartAsync(workflow, input, cancellation);
    }

    private async Task<TaskOutput> StartAsync(AutomationWorkflow workflow, JToken? input, CancellationToken? cancellation)
    {
        WorkflowContext context = new WorkflowContext(workflow);
        var tasks = new List<Task>();
        foreach (var start in workflow.Graph.GetStartNodes())
        {
            _changes?.OnTaskStart(start, input, context);
            tasks.Add(NextAsync(start, context, input, cancellation));
        }

        await Task.WhenAll(tasks);
        return await EndAsync(context);
    }

    private async Task<TaskOutput> EndAsync(WorkflowContext context)
    {
        if (context.Workflow.OutputSchema != null && context.OutputToken == null)
            throw new Exception("Reached end of workflow without data.");

        return new TaskOutput()
        {
            OutputToken = context.OutputToken,
            State = EnumTaskState.Completed
        };
    }

    private async Task NextAsync(BaseGraphTask task, WorkflowContext context, JToken? previous, CancellationToken? cancellation)
    {
        var nextTasks = context.Workflow.Graph.GetNextFrom(task);
        var outputs = new List<Task>();

        foreach (var next in nextTasks)
        {
            if (next.Settings.IsWaitingAllInputs)
                next.WaitedInputs.Add(task.Name, previous);
    
            if (context.Workflow.Graph.CanExecute(next))
            {
                // Capture 'next' for the async closure
                var nextTask = next;
                outputs.Add(Task.Run(async () =>
                {
                    var taskContext = context.Workflow.Graph.Execution.GetContextFor(next, previous, context.SharedToken);
                    if (next.Settings.IsWaitingAllInputs)
                        next.WaitedInputs.Clear();

                    JToken? input = null;
                    if (!string.IsNullOrEmpty(next.InputJson))
                        input = ReferencesHandler.ReplaceReferences(JToken.Parse(next.InputJson), taskContext).ReplacedSetting;

                    _changes?.OnTaskStart(next, previous, context);
                    var output = await _executor.ExecuteAsync(next.AutomationTask ?? throw new Exception("Workflow tasks are not loaded."), input, context, null, cancellation);
                    _changes?.OnTaskEnd(next, output, context);

                    if (output.State == EnumTaskState.Completed)
                        await NextAsync(nextTask, context, output.OutputToken, cancellation);
                }));
            }
        }
        await Task.WhenAll(outputs);
    }
}