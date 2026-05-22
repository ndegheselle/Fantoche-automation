using Automation.Plugins.Shared;
using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Graph;
using Automation.Shared.Data.Scoped;
using Newtonsoft.Json.Linq;

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
    public Dictionary<Guid, List<NodeInstance>> NodeInstances { get; } = [];

    private readonly HashSet<Guid> _claimedWaitAll = [];
    private readonly object _lock = new();

    public WorkflowContext(AutomationWorkflow workflow, JToken? sharedToken = null)
    {
        Workflow = workflow;
        SharedToken = sharedToken;
    }

    public void AddInstance(BaseGraphTask node, NodeInstance instance)
    {
        lock (_lock)
        {
            if (!NodeInstances.TryGetValue(node.Id, out var list))
                NodeInstances[node.Id] = list = [];
            list.Add(instance);
        }
    }

    /// <summary>
    /// Atomically check that every previous graph node of <paramref name="node"/> has at
    /// least one instance and claim the right to launch the node. Returns true to exactly
    /// one caller so concurrent predecessors can't fire a wait-all node twice.
    /// </summary>
    public bool TryClaimWaitAll(BaseGraphTask node)
    {
        var previous = Workflow.Graph.GetPreviousFrom(node);
        lock (_lock)
        {
            if (_claimedWaitAll.Contains(node.Id))
                return false;
            if (!previous.All(p => NodeInstances.TryGetValue(p.Id, out var list) && list.Count > 0))
                return false;
            _claimedWaitAll.Add(node.Id);
            return true;
        }
    }

    public IReadOnlyList<NodeInstance> GetPreviousInstances(BaseGraphTask node)
    {
        var previous = Workflow.Graph.GetPreviousFrom(node);
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
            var startInstance = CreateInstance(start, input, input, EnumTaskState.Completed);
            context.AddInstance(start, startInstance);
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

            // Gate wait-all nodes: only the predecessor that completes the set fires it
            if (next.Settings.IsWaitingAllInputs && !context.TryClaimWaitAll(next))
                continue;

            branches.Add(Task.Run(() => RunBranchAsync(next, currentInstance, context, cancellation)));
        }

        if (branches.Count > 0)
            await Task.WhenAll(branches);
    }

    private async Task RunBranchAsync(
        BaseGraphTask next,
        NodeInstance previousInstance,
        WorkflowContext context,
        CancellationToken? cancellation)
    {
        var previousInstances = next.Settings.IsWaitingAllInputs
            ? context.GetPreviousInstances(next)
            : new[] { previousInstance };

        var taskContext = context.Workflow.Graph.Execution.GetContextFor(next, previousInstances, context.SharedToken);

        JToken? input = null;
        if (!string.IsNullOrEmpty(next.InputJson))
            input = ReferencesHandler.ReplaceReferences(JToken.Parse(next.InputJson), taskContext).ReplacedSetting;

        // End tasks don't execute: their input becomes the workflow output piece
        if (next.TaskId == AutomationControl.EndTask.Id)
        {
            var endInstance = CreateInstance(next, input, input, EnumTaskState.Completed);
            Link(previousInstances, endInstance);
            context.AddInstance(next, endInstance);
            return;
        }

        var output = await _executor.ExecuteAsync(
            next.AutomationTask ?? throw new Exception("Workflow tasks are not loaded (is the graph refreshed?)."),
            input,
            null,
            cancellation);

        var instance = CreateInstance(next, input, output.OutputToken, output.State);
        Link(previousInstances, instance);
        context.AddInstance(next, instance);

        if (output.State == EnumTaskState.Completed)
            await NextAsync(next, instance, context, output.ActiveOutputConnectorIds, cancellation);
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
            OutputToken = CombineEndOutputs(endInstances, context.Workflow.WorkflowSettings),
            State = EnumTaskState.Completed
        };
    }

    private static JToken? CombineEndOutputs(IReadOnlyList<NodeInstance> endInstances, WorkflowSettings settings)
    {
        if (endInstances.Count == 0)
            return null;

        // Each end instance carries its input as its output (see RunBranchAsync)
        if (settings.StopAtFirstEnd)
            return endInstances.OrderBy(x => x.FinishedAt ?? x.CreatedAt).First().Output;

        if (endInstances.Count == 1)
            return endInstances[0].Output;

        // Merge object outputs together, fall back to an array for heterogeneous tokens
        if (endInstances.All(x => x.Output is JObject))
        {
            var merged = new JObject();
            foreach (var inst in endInstances)
                merged.Merge(inst.Output, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Concat });
            return merged;
        }

        return new JArray(endInstances.Select(x => x.Output).Where(x => x != null));
    }

    private static NodeInstance CreateInstance(BaseGraphTask node, JToken? input, JToken? output, EnumTaskState state)
    {
        return new NodeInstance
        {
            GraphNodeId = node.Id,
            TaskId = node.TaskId,
            Name = node.Name,
            Input = input,
            Output = output,
            State = state,
            FinishedAt = DateTime.UtcNow,
        };
    }

    private static void Link(IEnumerable<NodeInstance> previous, NodeInstance instance)
    {
        foreach (var p in previous)
        {
            instance.Previous.Add(p);
            p.Nexts.Add(instance);
        }
    }
}
