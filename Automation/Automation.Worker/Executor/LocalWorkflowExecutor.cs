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
            tasks.Add(NextAsync(start, context, input, null, cancellation));
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

    private async Task NextAsync(BaseGraphTask task, WorkflowContext context, JToken? previous, HashSet<Guid>? activeOutputConnectorIds, CancellationToken? cancellation)
    {
        var nextPairs = context.Workflow.Graph.GetNext(task);

        // When a control task activates only specific outputs, skip the rest
        if (activeOutputConnectorIds != null)
            nextPairs = nextPairs.Where(x => activeOutputConnectorIds.Contains(x.SourceConnector.Id));

        var outputs = new List<Task>();

        foreach (var next in nextPairs)
        {
            if (next.Task.Settings.IsWaitingAllInputs)
                next.Task.WaitedInputs.Add(task.Name, previous);

            if (context.Workflow.Graph.CanExecute(next.Task))
            {
                // Capture 'next' for the async closure
                var nextTask = next;
                outputs.Add(Task.Run(async () =>
                {
                    var taskContext = context.Workflow.Graph.Execution.GetContextFor(next.Task, previous, context.SharedToken);
                    if (next.Task.Settings.IsWaitingAllInputs)
                        next.Task.WaitedInputs.Clear();

                    JToken? input = null;
                    if (!string.IsNullOrEmpty(next.Task.InputJson))
                        input = ReferencesHandler.ReplaceReferences(JToken.Parse(next.Task.InputJson), taskContext).ReplacedSetting;

                    _changes?.OnTaskStart(next.Task, previous, context);
                    var output = await _executor.ExecuteAsync(next.Task.AutomationTask ?? throw new Exception("Workflow tasks are not loaded."), input, context, next.Task, null, cancellation);
                    _changes?.OnTaskEnd(next.Task, output, context);

                    if (output.State == EnumTaskState.Completed)
                        await NextAsync(nextTask.Task, context, output.OutputToken, output.ActiveOutputConnectorIds, cancellation);
                }));
            }
        }
        await Task.WhenAll(outputs);
    }
}