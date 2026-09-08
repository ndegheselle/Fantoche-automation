using Automation.Shared.Data.Graph;
using Newtonsoft.Json.Linq;

namespace Automation.Shared.Data.Execution;

/// <summary>
/// Allow an editor to simulate the execution of a workflow to get all the potential errors and resolved references.
/// </summary>
public class GraphExecutionPreview
{
    public Dictionary<Guid, List<string>> NodesErrors { get; } = [];

    public void BuildSamples(TasksGraph graph, GraphContextResolution resolution)
    {
        if (graph.IsRefreshed == false)
            throw new Exception("The graph need to be refreshed before a preview.");

        foreach (var start in graph.GetStartNodes())
        {
            // Merge schema sample with default values
            var workflowParameters = start.OutputSchema?.ToSampleJson();
            var startInstance = resolution.CreateInstance(start, workflowParameters, EnumTaskState.Completed);
            startInstance.Output = GraphContextResolution.MergeContexts(start.InputTemplate, workflowParameters);

            GraphExecutionContext context = new()
            {
                Resolution = resolution,
                Graph = graph,
                Node = start,
                Instance = startInstance,
            };

            foreach (var nextNode in graph.GetNext(start))
                RunBranch(context with { Node = nextNode.Task }, new JObject());
        }
    }

    /// <summary>
    /// Walk the branch [context] stands at, previewing every node it leads to.
    /// </summary>
    private void RunBranch(GraphExecutionContext context, JToken? sharedContext)
    {
        // Control nodes are driven by the workflow itself, they stand for no task to preview.
        TaskInstance? instance = context.Node is GraphControl control
            ? PreviewControl(context, control, sharedContext)
            : PreviewTask(context, sharedContext);

        // A join still waiting for its other branches, or a node already previewed.
        if (instance == null)
            return;

        // XXX : the shared context is threaded along the walk rather than carried by the instances
        // the way a run carries it (see WorkflowExecutor.RunBranchAsync), so what a share resolved
        // is discarded here and reset on every node : "$shared" previews as empty. The two lines
        // below are the whole of it — the rest already resolves the shared context the way a run
        // does.
        instance.Shared = sharedContext;
        foreach (var nextNode in context.Graph.GetNext(context.Node))
            RunBranch(context with { Node = nextNode.Task, Instance = instance }, new JObject());
    }

    #region Control tasks
    /// <summary>
    /// Preview a control node : it has no task to run, only a mapping to resolve. A join holds the
    /// branch until every other one reached it, the rest of the controls are passed through by each
    /// branch reaching them.
    /// </summary>
    private TaskInstance? PreviewControl(GraphExecutionContext context, GraphControl control, JToken? sharedContext)
    {
        TaskInstance? instance = control.IsJoin()
            ? PreviewJoin(context, control, sharedContext)
            : PreviewSingleBranchControl(context, control, sharedContext);

        if (instance == null)
            return null;

        // A control produces nothing of its own, it hands over its resolved parameters.
        instance.Output = instance.Parameters ?? new JObject();
        instance.State = EnumTaskState.Completed;

        // The end closes the branch : it declares no output connector, so the walk stops on its own.
        return instance;
    }

    /// <summary>
    /// Preview a control reached by a single branch (a share, a map or the end) : it resolves
    /// against what that branch carries.
    /// </summary>
    private TaskInstance? PreviewSingleBranchControl(GraphExecutionContext context, GraphControl control, JToken? sharedContext)
    {
        // We pass on each node exactly once
        if (context.Resolution.NodeInstances.ContainsKey(control.Id))
            return null;

        var input = context.Resolution.GetInputFor(control, context.Instance, sharedContext);
        if (input.HasError)
        {
            AddErrors(control.Id, input.Errors);
            return null;
        }

        var instance = context.Resolution.CreateInstance(control, input.Token, EnumTaskState.Progressing, context.Instance);
        // A share feeds what it resolved to everything downstream, the other controls hand the
        // shared context over as they got it.
        instance.Shared = control.IsShare()
            ? GraphContextResolution.MergeContexts(sharedContext, input.Token)
            : sharedContext;

        return instance;
    }

    /// <summary>
    /// Preview a join, null while some of its branches have yet to reach it : the last one arriving
    /// resumes the waiting instance with what every branch produced.
    /// </summary>
    private TaskInstance? PreviewJoin(GraphExecutionContext context, GraphControl control, JToken? sharedContext)
    {
        var previousNodes = context.Graph.GetPrevious(control).ToList();

        if (!context.Resolution.TryJoinBranches(control, context.Instance, previousNodes, out var instance, out var branchesInstances))
            return null;

        // Merge every branche shared data
        JToken? shared = branchesInstances.Aggregate(
            sharedContext,
            (merged, branch) => GraphContextResolution.MergeContexts(merged, branch.Shared));

        var input = context.Resolution.GetInputFor(control, branchesInstances, shared);
        if (input.HasError)
        {
            AddErrors(control.Id, input.Errors);
            return null;
        }

        // The branch resuming the join is the one it carries on from.
        instance.Previous = context.Instance;
        instance.Parameters = input.Token;
        instance.Shared = shared;
        instance.State = EnumTaskState.Progressing;

        return instance;
    }
    #endregion

    private TaskInstance? PreviewTask(GraphExecutionContext context, JToken? sharedContext)
    {
        // We pass on each node exactly once
        if (context.Resolution.NodeInstances.ContainsKey(context.Node.Id))
            return null;

        // We don't really need the input here, we just check if there is any errors in mapping
        var result = context.Resolution.GetInputFor(context.Node, context.Instance, sharedContext);
        if (result.HasError)
        {
            AddErrors(context.Node.Id, result.Errors);
            return null;
        }

        var instance = context.Resolution.CreateInstance(context.Node, result.Token, EnumTaskState.Completed, context.Instance);
        instance.Output = context.Node.OutputSchema?.ToSampleJson();

        return instance;
    }

    /// <summary>
    /// Add what is wrong with [nodeId] to what is already known of it : a node reached by several
    /// branches has its mapping resolved once per branch, and a branch failing to resolve it leaves
    /// no instance behind for the next one to stop on.
    /// </summary>
    private void AddErrors(Guid nodeId, IEnumerable<string> errors)
    {
        if (!NodesErrors.TryGetValue(nodeId, out List<string>? known))
            NodesErrors[nodeId] = known = [];

        foreach (string error in errors)
        {
            if (!known.Contains(error))
                known.Add(error);
        }
    }
}
