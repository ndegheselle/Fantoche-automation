using Automation.Plugins.Shared;
using Automation.Shared.Data;
using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Graph;
using Automation.Shared.Data.Scoped;
using Newtonsoft.Json.Linq;

namespace Automation.Worker.Executor;

public class WorkflowContext
{
    public AutomationWorkflow Workflow { get; }
    /// <summary>
    /// Shared context between task, initalized with workflow parent context.
    /// </summary>
    public JToken? SharedToken { get; }

    public Dictionary<Guid, List<NodeInstance>> NodeInstances { get; } = [];

    public WorkflowContext(AutomationWorkflow workflow)
    {
        Workflow = workflow;
    }
}

public class LocalWorkflowExecutor
{
    private readonly LocalTaskExecutor _executor;

    public LocalWorkflowExecutor(LocalTaskExecutor executor)
    {
        _executor = executor;
    }

    public async Task<TaskOutput> ExecuteAsync(
        AutomationWorkflow workflow,
        ExecutionContext context,
        IProgress<TaskNotification>? notifications = null,
        CancellationToken? cancellation = null)
    {
        if (workflow.Graph.IsRefreshed == false)
            throw new Exception("The workflow graph should be refreshed before being executed.");

        return await StartAsync(workflow, context, cancellation);
    }

    private async Task<TaskOutput> StartAsync(AutomationWorkflow workflow, ExecutionContext context, CancellationToken? cancellation)
    {
        WorkflowContext workflowContext = new WorkflowContext(workflow);
        var tasks = new List<Task>();
        foreach (var start in workflow.Graph.GetStartNodes())
        {
            tasks.Add(NextAsync(start, workflowContext, context.Input, null, cancellation));
        }

        await Task.WhenAll(tasks);
        return await EndAsync(workflowContext);
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

    private async Task NextAsync(
        BaseGraphTask task, 
        WorkflowContext workflowContext, 
        JToken? previous, 
        HashSet<Guid>? activeOutputConnectorIds, 
        CancellationToken? cancellation)
    {
        var nextPairs = workflowContext.Workflow.Graph.GetNext(task);

        // When a control task activates only specific outputs, skip the rest
        if (activeOutputConnectorIds != null)
            nextPairs = nextPairs.Where(x => activeOutputConnectorIds.Contains(x.SourceConnector.Id));

        var nextTasks = nextPairs.Select(x => x.Task);

        if (!nextTasks.Any())
            return [];
        var branchTasks = new List<Task<IEnumerable<JToken?>>>();

        foreach (var next in nextTasks)
        {
            if (next.Settings.IsWaitingAllInputs)
                next.WaitedInputs.Add(task.Name, previous);

            if (!workflowContext.Workflow.Graph.CanExecute(next))
                continue;
            
            // Capture 'next' for the async closure
            var nextTask = next;
            branchTasks.Add(Task.Run(async () =>
            {
                var taskContext = workflowContext.Workflow.Graph.Execution.GetContextFor(next, previous, workflowContext.SharedToken);
                if (next.Settings.IsWaitingAllInputs)
                    next.WaitedInputs.Clear();

                JToken? input = null;
                if (!string.IsNullOrEmpty(next.InputJson))
                    input = ReferencesHandler.ReplaceReferences(JToken.Parse(next.InputJson), taskContext).ReplacedSetting;

                if (next.TaskId == AutomationControl.EndTask.Id)
                {
                    return [await HandleEndTask(input)];
                }

                var output = await _executor.ExecuteAsync(
                    next.AutomationTask ?? throw new Exception("Workflow tasks are not loaded (is the graph refreshed ?)."),
                    new ExecutionContext()
                    {
                        GraphNode = next,
                        WorkflowContext = workflowContext,
                        Input = input
                    },
                    null,
                    cancellation);

                if (output.State == EnumTaskState.Completed)
                    return await NextAsync(nextTask, workflowContext, output.OutputToken, output.ActiveOutputConnectorIds, cancellation);

                // Branch didn't complete (e.g. faulted/skipped)
                return [];
            }));
            
        }

        if (!branchTasks.Any())
            return [];

        var results = await Task.WhenAll(branchTasks);

        // Flatten all branch results into a single collection
        return results.Where(x => x != null).SelectMany(x => x);
    }

    public Task<JToken?> HandleEndTask(JToken? input)
    {
        // TODO : stop all others tasks of the workflow

        // Pass the input of the end task as the output of the workflow
        return Task.FromResult(input);
    }
}