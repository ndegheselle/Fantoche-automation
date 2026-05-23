using Automation.Plugins.Shared;
using Automation.Shared.Data;
using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Graph;
using Automation.Shared.Data.Scoped;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Numerics;

namespace Automation.Worker.Executor;

public class WorkflowContext
{
    public AutomationWorkflow Workflow { get; }

    /// <summary>
    /// Shared context between tasks, initialized with the workflow parent context.
    /// </summary>
    public JToken? SharedToken { get; }

    /// <summary>
    /// Instances created during this workflow execution, indexed by graph node id.
    /// </summary>
    public ConcurrentDictionary<Guid, List<NodeInstance>> NodeInstances { get; } = [];

    private readonly object _lock = new();

    public WorkflowContext(AutomationWorkflow workflow, JToken? sharedToken = null)
    {
        Workflow = workflow;
        SharedToken = sharedToken;
    }

    public NodeInstance CreateInstance(BaseGraphTask node, JToken? input, EnumTaskState state = EnumTaskState.Pending, NodeInstance? previous = null)
    {
        var instance = new NodeInstance
        {
            GraphNodeId = node.Id,
            TaskId = node.TaskId,
            Name = node.Name,
            Input = input,
            State = state,
            FinishedAt = (state & EnumTaskState.Finished) != 0 ? DateTime.UtcNow : null,
        };

        if (previous != null)
            instance.Link(previous);

        lock (_lock)
        {
            if (!NodeInstances.TryGetValue(node.Id, out var list))
                NodeInstances[node.Id] = list = [];
            list.Add(instance);
        }

        // TODO : call event to notify instance change

        return instance;
    }

    public NodeInstance UpdateInstance(NodeInstance instance, JToken? input, JToken? output, EnumTaskState state)
    {
        instance.Input = input;
        instance.Output = output;
        instance.State = state;

        // TODO : call event to notify instance change
        return instance;
    }

    public NodeInstance? GetLastNodeInstance(BaseGraphTask node, EnumTaskState state)
    {
        NodeInstances.TryGetValue(node.Id, out var list);
        var lastInstance = list?.OrderBy(x => x.FinishedAt).FirstOrDefault(i => (i.State & state) == 0);
        return lastInstance;
    }

    /// <summary>
    /// Ensures a Waiting instance exists for this wait-all node, and atomically claims it
    /// (transitions to Progressing) when all predecessors have run. Returns the claimed instance
    /// to exactly one caller; all others get null.
    /// </summary>
    public IReadOnlyList<NodeInstance>? TryGetAllPrevious(BaseGraphTask node)
    {
        var previous = Workflow.Graph.GetPrevious(node);
        lock (_lock)
        {
            List<NodeInstance> previousInstances = [];
            foreach(var p in previous)
            {
                var previousInstance = GetLastNodeInstance(p, EnumTaskState.Completed);
                // If one of the previous doesn't have a completed node instance we stop there
                if (previousInstance == null)
                    return null;
                previousInstances.Add(previousInstance);
            }

            return previousInstances;
        }
    }

    public IReadOnlyList<NodeInstance> GetPreviousInstances(BaseGraphTask node)
    {
        var previous = Workflow.Graph.GetPrevious(node);
        lock (_lock)
        {
            return previous
                .Where(p => NodeInstances.ContainsKey(p.Id))
                .SelectMany(p => NodeInstances[p.Id])
                .ToList();
        }
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
        JToken? input,
        JToken? sharedToken = null,
        IProgress<TaskNotification>? notifications = null,
        CancellationToken? cancellation = null)
    {
        if (workflow.Graph.IsRefreshed == false)
            throw new Exception("The workflow graph should be refreshed before being executed.");

        var context = new WorkflowContext(workflow, sharedToken);

        var startTasks = new List<Task>();
        foreach (var start in workflow.Graph.GetStartNodes())
        {
            var startInstance = context.CreateInstance(start, input, EnumTaskState.Completed);
            startTasks.Add(NextAsync(start, startInstance, context, null, cancellation));
        }

        await Task.WhenAll(startTasks);
        return EndAsync(context);
    }

    private async Task NextAsync(
        BaseGraphTask current,
        NodeInstance currentInstance,
        WorkflowContext context,
        HashSet<Guid>? activeOutputConnectorIds,
        CancellationToken? cancellation)
    {
        var nextPairs = context.Workflow.Graph.GetNext(current);

        // When a control task activates only specific outputs, skip the rest
        if (activeOutputConnectorIds != null)
            nextPairs = nextPairs.Where(x => activeOutputConnectorIds.Contains(x.SourceConnector.Id));

        var branches = new List<Task>();
        foreach (var pair in nextPairs)
        {
            var next = pair.Task;

            
            if (next.TaskId == AutomationControl.EndTask.Id)
            {
                // TODO : Check if end (workflow setting, or ignore if the workflow wait on all end)

                // If context.Workflow.WorkflowSettings.StopAtFirstEnd return there and cancel other tasks
                return;
            }

            branches.Add(Task.Run(() => RunBranchAsync(next, currentInstance, context, cancellation)));
        }

        if (branches.Count > 0)
            await Task.WhenAll(branches);
    }

    private async Task RunBranchAsync(
        BaseGraphTask task,
        NodeInstance previousInstance,
        WorkflowContext context,
        CancellationToken? cancellation)
    {
        NodeInstance? existingInstance = null;
        IReadOnlyList<NodeInstance>? previousInstances = null;
        // If the task wait for all inputs but only have one we treat it like other tasks and skip this part
        if (task.Settings.IsWaitingAllInputs && context.Workflow.Graph.WithMultipleInputsConnections(task))
        {
            existingInstance = context.GetLastNodeInstance(task, EnumTaskState.Waiting) ?? 
                context.CreateInstance(task, null, EnumTaskState.Waiting, previousInstance);

            previousInstances = context.TryGetAllPrevious(task);
            // All previous are not ready yet
            if (previousInstances == null || previousInstances.Count() == 0)
                return;

            foreach (var prev in previousInstances)
                existingInstance.Link(prev);
        }

        previousInstances ??= [previousInstance];
        JToken? input = null;
        if (!string.IsNullOrEmpty(task.InputJson))
        {
            var taskContext = context.Workflow.Graph.Execution.GetContextFor(task, previousInstances, context.SharedToken);
            input = ReferencesHandler.ReplaceReferences(JToken.Parse(task.InputJson), taskContext).ReplacedSetting;
        }

        // Set the instance as progressing
        if (existingInstance == null)
            existingInstance = context.CreateInstance(task, input, EnumTaskState.Progressing, previousInstance);
        else
            existingInstance = context.UpdateInstance(existingInstance, input, null, EnumTaskState.Progressing);

        var output = await _executor.ExecuteAsync(
            task.AutomationTask ?? throw new Exception("Workflow tasks are not loaded (is the graph refreshed?)."),
            input,
            null,
            cancellation);

        // Set the end state of the instance
        existingInstance = context.UpdateInstance(existingInstance, input, output.OutputToken, output.State);

        if (output.State == EnumTaskState.Completed)
            await NextAsync(task, existingInstance, context, output.ActiveOutputConnectorIds, cancellation);
    }

    private TaskOutput EndAsync(WorkflowContext context)
    {
        var endInstances = new List<NodeInstance>();
        foreach (var endNode in context.Workflow.Graph.GetEndNodes())
        {
            if (context.NodeInstances.TryGetValue(endNode.Id, out var list))
                endInstances.AddRange(list);
        }

        if (context.Workflow.OutputSchema != null && endInstances.Count == 0)
            throw new Exception("Reached end of workflow without data.");

        return new TaskOutput
        {
            OutputToken = context.Workflow.Graph.Execution.CombineEndOutputs(endInstances, context.Workflow.WorkflowSettings),
            State = EnumTaskState.Completed
        };
    }
}
