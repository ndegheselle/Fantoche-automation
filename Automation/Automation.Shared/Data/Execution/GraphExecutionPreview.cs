using Automation.Shared.Data.Graph;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Automation.Shared.Data.Execution;

/// <summary>
/// What is wrong with one node of a graph, and which branch it is wrong on.
/// </summary>
public record GraphPreviewError
{
    public required string Message { get; init; }

    /// <summary>
    /// The edges the failing context travelled to get there, in walk order — the last one leads to
    /// the node holding the error. A path starts at the start of the workflow or at the last join it
    /// went through : a join is reached by every branch at once, so where it carries on from is no
    /// longer a single path.
    /// </summary>
    public required IReadOnlyList<GraphEdge> Provenance { get; init; }

    /// <summary>
    /// The edge to blame : the closest one on <see cref="Provenance"/> leading to a node another
    /// branch also leads to, so where this path became distinguishable from the ones holding up.
    /// <para>
    /// Null when nothing on the path branches, which means the node never resolves whichever way it
    /// is reached — there is nothing to point at but the node itself.
    /// </para>
    /// </summary>
    public GraphEdge? DivergenceEdge { get; init; }

    /// <summary>
    /// Whether [other] is the same thing wrong with the same branch : the same error found again
    /// down another path is another thing to show.
    /// </summary>
    public bool IsSameAs(GraphPreviewError other)
        => Message == other.Message && Provenance.SequenceEqual(other.Provenance);
}

/// <summary>
/// Allow an editor to simulate the execution of a workflow to get all the potential errors and resolved references.
/// <para>
/// A node is previewed once per context it can be reached with rather than once per node : a mapping
/// valid from one branch and broken from another one is exactly what an editor cannot see by reading
/// the graph. The contexts are held by value, so the paths of the graph collapse into the far smaller
/// set of things they actually carry, and a graph looping back on itself stops as soon as it stops
/// carrying something new.
/// </para>
/// <para>
/// One instance serves one <see cref="BuildSamples"/> call.
/// </para>
/// </summary>
public class GraphExecutionPreview
{
    /// <summary>
    /// How many distinct contexts a single node is previewed with before the walk gives up on it.
    /// A graph whose branches all carry something different can reach a node in more ways than an
    /// editor can show : the preview stays bounded and says so with <see cref="IsTruncated"/> rather
    /// than hanging the edition.
    /// </summary>
    public const int MaxContextsPerNode = 16;

    /// <summary>
    /// What is wrong with the nodes of the graph, by node id.
    /// </summary>
    public Dictionary<Guid, List<GraphPreviewError>> NodesErrors { get; } = [];

    /// <summary>
    /// The branches a node cannot handle, by edge : what an editor draws in red to tell taking that
    /// edge apart from taking another one. An error the node holds whichever branch reaches it is
    /// only in <see cref="NodesErrors"/>, see <see cref="GraphPreviewError.DivergenceEdge"/>.
    /// </summary>
    public Dictionary<GraphEdge, List<GraphPreviewError>> EdgesErrors { get; } = [];

    /// <summary>
    /// Whether a node reached <see cref="MaxContextsPerNode"/>, which means some of the ways the
    /// graph can run are not accounted for by what this holds.
    /// </summary>
    public bool IsTruncated { get; private set; }

    /// <summary>
    /// The contexts a node was already previewed with, by node id : what they hold against it, null
    /// when it resolved. Both what keeps the walk from previewing the same thing twice and what
    /// makes it terminate.
    /// </summary>
    private readonly Dictionary<Guid, Dictionary<string, List<string>?>> _contexts = [];

    /// <summary>
    /// The node an edge leads to, by edge. Held rather than looked up : the path of a failing
    /// context is walked backwards looking for the branches of the graph.
    /// </summary>
    private readonly Dictionary<GraphEdge, BaseGraphTask> _edgeTargets = [];

    public void BuildSamples(TasksGraph graph, GraphContextResolution resolution)
    {
        if (graph.IsRefreshed == false)
            throw new Exception("The graph need to be refreshed before a preview.");

        foreach (GraphConnection connection in graph.Connections)
            _edgeTargets[connection.Edge] = connection.Target!.Parent!;

        // The nodes left to preview. A queue rather than a recursion : a node is reached as many
        // times as the graph carries something new to it, and a join only resolves once every branch
        // before it did — which no order of walking a graph can be relied on to do.
        Queue<PreviewStep> pending = new();

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

            EnqueueNext(pending, context, []);
        }

        while (pending.TryDequeue(out PreviewStep? step))
        {
            TaskInstance? instance = PreviewNode(step);

            // Nothing new : the node was already previewed with what this path carries, it failed to
            // resolve, or it is a join still waiting for its other branches.
            if (instance == null)
                continue;

            // A join is reached by every branch at once, so the path it carries on from restarts
            // there : blaming an edge before it would be naming one of the branches arbitrarily.
            IReadOnlyList<GraphEdge> provenance = step.Context.Node is GraphControl control && control.IsJoin()
                ? []
                : step.Provenance;

            EnqueueNext(pending, step.Context with { Instance = instance }, provenance);
        }
    }

    private static void EnqueueNext(Queue<PreviewStep> pending, GraphExecutionContext context, IReadOnlyList<GraphEdge> provenance)
    {
        foreach (GraphSource next in context.Graph.GetNext(context.Node))
        {
            pending.Enqueue(new PreviewStep
            {
                Context = context with { Node = next.Task },
                // The shared context walks down the branch, whatever a share upstream fed it with.
                Shared = context.Instance.Shared,
                Provenance = [.. provenance, next.Connection.Edge],
            });
        }
    }

    private TaskInstance? PreviewNode(PreviewStep step)
    {
        // Control nodes are driven by the workflow itself, they stand for no task to preview.
        return step.Context.Node is GraphControl control
            ? PreviewControl(step, control)
            : PreviewTask(step);
    }

    #region Control tasks
    /// <summary>
    /// Preview a control node : it has no task to run, only a mapping to resolve. A join holds the
    /// branch until every other one reached it, the rest of the controls are passed through by each
    /// branch reaching them.
    /// </summary>
    private TaskInstance? PreviewControl(PreviewStep step, GraphControl control)
    {
        TaskInstance? instance = control.IsJoin()
            ? PreviewJoin(step, control)
            : PreviewSingleBranchControl(step, control);

        if (instance == null)
            return null;

        // A control produces nothing of its own, it hands over its resolved parameters.
        instance.Output = instance.Parameters ?? new JObject();
        instance.State = EnumTaskState.Completed;

        // The end closes the branch : it declares no output connector, so the walk stops on its own.
        return instance;
    }

    /// <summary>
    /// Preview a control reached by a single branch (a share, a map or the end) : it resolves against
    /// what that branch carries.
    /// </summary>
    private TaskInstance? PreviewSingleBranchControl(PreviewStep step, GraphControl control)
    {
        GraphExecutionContext context = step.Context;

        string contextKey = ContextKey([context.Instance], step.Shared);
        if (!TryTakeContext(step, control.Id, contextKey))
            return null;

        var input = context.Resolution.GetInputFor(control, context.Instance, step.Shared);
        if (input.HasError)
        {
            Fail(step, control.Id, contextKey, input.Errors);
            return null;
        }

        var instance = context.Resolution.CreateInstance(control, input.Token, EnumTaskState.Progressing, context.Instance);
        // A share feeds what it resolved to everything downstream, the other controls hand the
        // shared context over as they got it.
        instance.Shared = control.IsShare()
            ? GraphContextResolution.MergeContexts(step.Shared, input.Token)
            : step.Shared;

        return instance;
    }

    /// <summary>
    /// Preview a join, null while some of its branches have yet to reach it : the last one arriving
    /// resumes the waiting instance with what every branch produced.
    /// </summary>
    private TaskInstance? PreviewJoin(PreviewStep step, GraphControl control)
    {
        GraphExecutionContext context = step.Context;
        var previousNodes = context.Graph.GetPrevious(control).ToList();

        if (!context.Resolution.TryJoinBranches(control, context.Instance, previousNodes, out var instance, out var branchesInstances))
            return null;

        // Merge every branche shared data
        JToken? shared = branchesInstances.Aggregate(
            step.Shared,
            (merged, branch) => GraphContextResolution.MergeContexts(merged, branch.Shared));

        // Every branch reaching the join resolves it against the same instances, so it is previewed
        // by whichever of them gets there once they are all in.
        string contextKey = ContextKey(branchesInstances, shared);
        if (!TryTakeContext(step, control.Id, contextKey))
            return null;

        var input = context.Resolution.GetInputFor(control, branchesInstances, shared);
        if (input.HasError)
        {
            Fail(step, control.Id, contextKey, input.Errors);
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

    private TaskInstance? PreviewTask(PreviewStep step)
    {
        GraphExecutionContext context = step.Context;

        string contextKey = ContextKey([context.Instance], step.Shared);
        if (!TryTakeContext(step, context.Node.Id, contextKey))
            return null;

        // We don't really need the input here, we just check if there is any errors in mapping
        var result = context.Resolution.GetInputFor(context.Node, context.Instance, step.Shared);
        if (result.HasError)
        {
            Fail(step, context.Node.Id, contextKey, result.Errors);
            return null;
        }

        var instance = context.Resolution.CreateInstance(context.Node, result.Token, EnumTaskState.Completed, context.Instance);
        instance.Shared = step.Shared;
        instance.Output = context.Node.OutputSchema?.ToSampleJson();

        return instance;
    }

    #region Contexts
    /// <summary>
    /// What a node reads, as a value : two contexts holding the same thing preview the same, so only
    /// one of them is followed. Built out of what <see cref="GraphContextResolution"/> builds the
    /// context itself with — what the branches before it hand over, named the way their mappings name
    /// them, and the shared context.
    /// </summary>
    private static string ContextKey(IReadOnlyList<TaskInstance> previous, JToken? shared)
    {
        // Named even when a single branch reaches the node, which does not name it : two branches
        // handing over the same thing are still two branches to blame separately.
        IEnumerable<string> branches = previous
            .Select(x => x.Effective)
            .Select(x => $"{x.NodeName}={x.Output?.ToString(Formatting.None)}");

        return $"{string.Join('|', branches)}#{shared?.ToString(Formatting.None)}";
    }

    /// <summary>
    /// Whether [nodeId] has yet to be previewed with [contextKey]. False when it already was — what
    /// it held against the node is then held against the path [step] came by as well — and false when
    /// the node reached <see cref="MaxContextsPerNode"/>.
    /// </summary>
    private bool TryTakeContext(PreviewStep step, Guid nodeId, string contextKey)
    {
        if (!_contexts.TryGetValue(nodeId, out Dictionary<string, List<string>?>? previewed))
            _contexts[nodeId] = previewed = [];

        if (previewed.TryGetValue(contextKey, out List<string>? errors))
        {
            if (errors != null)
                AddErrors(step, nodeId, errors);
            return false;
        }

        if (previewed.Count >= MaxContextsPerNode)
        {
            IsTruncated = true;
            return false;
        }

        previewed[contextKey] = null;
        return true;
    }
    #endregion

    #region Errors
    /// <summary>
    /// Hold [messages] against the node and against the branch it was reached by, and remember the
    /// context it fails on : another path carrying the same thing fails the same way, and is blamed
    /// for it without resolving the mapping again.
    /// </summary>
    private void Fail(PreviewStep step, Guid nodeId, string contextKey, List<string> messages)
    {
        _contexts[nodeId][contextKey] = messages;
        AddErrors(step, nodeId, messages);
    }

    private void AddErrors(PreviewStep step, Guid nodeId, IEnumerable<string> messages)
    {
        GraphEdge? divergence = FindDivergenceEdge(step);

        foreach (string message in messages)
        {
            GraphPreviewError error = new()
            {
                Message = message,
                Provenance = step.Provenance,
                DivergenceEdge = divergence,
            };

            if (!Add(NodesErrors, nodeId, error))
                continue;

            if (divergence.HasValue)
                Add(EdgesErrors, divergence.Value, error);
        }
    }

    /// <summary>
    /// Add [error] to what [key] already holds, false when it holds it already.
    /// </summary>
    private static bool Add<TKey>(Dictionary<TKey, List<GraphPreviewError>> errors, TKey key, GraphPreviewError error)
        where TKey : notnull
    {
        if (!errors.TryGetValue(key, out List<GraphPreviewError>? known))
            errors[key] = known = [];

        if (known.Any(x => x.IsSameAs(error)))
            return false;

        known.Add(error);
        return true;
    }

    /// <summary>
    /// The edge of the path [step] came by to blame, see <see cref="GraphPreviewError.DivergenceEdge"/>.
    /// </summary>
    private GraphEdge? FindDivergenceEdge(PreviewStep step)
    {
        TasksGraph graph = step.Context.Graph;

        // Walked backwards : the closest node another branch also leads to is where this path became
        // one of several, so where having taken it rather than another one is what went wrong.
        for (int i = step.Provenance.Count - 1; i >= 0; i--)
        {
            GraphEdge edge = step.Provenance[i];
            if (_edgeTargets.TryGetValue(edge, out BaseGraphTask? target) && graph.WithMultipleInputsConnections(target))
                return edge;
        }

        return null;
    }
    #endregion

    /// <summary>
    /// One node left to preview : where the walk stands (see <see cref="GraphExecutionContext"/>),
    /// what the shared context holds there and the path it was reached by.
    /// </summary>
    private record PreviewStep
    {
        public required GraphExecutionContext Context { get; init; }
        public required JToken? Shared { get; init; }

        /// <summary>See <see cref="GraphPreviewError.Provenance"/>.</summary>
        public required IReadOnlyList<GraphEdge> Provenance { get; init; }
    }
}
