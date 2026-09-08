using System.Collections.Concurrent;
using Automation.Shared.Data.Graph;
using Automation.Shared.Data.Scoped;
using Newtonsoft.Json.Linq;

namespace Automation.Shared.Data.Execution;

public class GraphContextResolutionException : Exception
{
    public GraphContextResolutionException(string message) : base(message)
    { }
}

/// <summary>
/// Where a walk of a graph stands : the node it reached, the instance it carries on from and what
/// every node already walked resolved to. Copied rather than mutated as the walk moves on (see the
/// <c>with</c> expression), so a branch never sees where another one went - what they do share is
/// held by <see cref="GraphContextResolution"/>.
/// <para>
/// Common to anything walking a graph : a run (see <c>WorkflowExecutor</c>) and the preview an
/// editor shows of it (see <see cref="GraphExecutionPreview"/>) only differ by what they do on each
/// node, not by what they need to know to get there.
/// </para>
/// </summary>
public record GraphExecutionContext
{
    /// <summary>
    /// The instances of the walk, and what the mappings of the nodes are resolved against.
    /// </summary>
    public required GraphContextResolution Resolution { get; init; }

    /// <summary>
    /// The graph being walked. Expected to be refreshed : the walk reads the nodes a connection
    /// leads to rather than the ids it holds.
    /// </summary>
    public required TasksGraph Graph { get; init; }

    /// <summary>
    /// The node the walk reached.
    /// </summary>
    public required BaseGraphTask Node { get; init; }

    /// <summary>
    /// The instance <see cref="Node"/> carries on from : the one of the node before it until it ran,
    /// its own once it did.
    /// </summary>
    public required TaskInstance Instance { get; init; }
}

public class GraphContextResult
{
    public JToken? Token { get; set; }
    public List<string> Errors { get; } = [];
    public bool HasError => Errors.Count > 0;
}

public class GraphContextResolution
{
    private const string PreviousIdentifier = "previous";
    private const string SharedIdentifier = "shared";
    private const string GlobalIdentifier = "global";

    private readonly Guid? _workflowInstanceId;
    public GraphContextResolution(Guid? workflowInstanceId = null)
    {
        _workflowInstanceId = workflowInstanceId;
    }

    public JToken? GlobalContext { get; set; }
    public ConcurrentDictionary<Guid, List<TaskInstance>> NodeInstances { get; } = [];

    /// <summary>
    /// Branches already arrived on a join, by node id. Guarded by <see cref="_joinsLock"/> : the
    /// branches of a join are resolved one at a time, only the last one arriving carries on.
    /// </summary>
    private readonly Dictionary<Guid, int> _joinBranches = [];
    private readonly object _joinsLock = new();

    public TaskInstance CreateInstance(BaseGraphTask node, JToken? parameters, EnumTaskState state = EnumTaskState.Pending, TaskInstance? previous = null)
    {
        TaskInstance instance;

        // A workflow reached as a node of another one is a run of its own : it walks its graph with
        // a resolution of its own, which the global context has to be handed over to.
        if (node.AutomationTask is AutomationWorkflow workflow)
            instance = new WorkflowInstance(workflow) { GlobalContext = GlobalContext };
        else
            instance = new TaskInstance();

        instance.ParentInstanceId = _workflowInstanceId;
        instance.TaskId = node.TaskId;
        instance.NodeId = node.Id;
        instance.NodeName = node.Name;
        instance.Node = node;
        instance.Parameters = parameters;
        instance.State = state;

        if (previous != null)
            instance.Link(previous);

        if (!NodeInstances.TryGetValue(node.Id, out var list))
            NodeInstances[node.Id] = list = [];
        list.Add(instance);

        return instance;
    }

    public TaskInstance GetOrCreateWaitingInstance(BaseGraphTask node, TaskInstance? previousInstance)
    {
        return GetLastInstance(node.Id, EnumTaskState.Waiting) ??
            CreateInstance(node, null, EnumTaskState.Waiting, previousInstance);
    }

    /// <summary>
    /// Register the branch reaching [node] and return whether it is the one resuming it : true once
    /// every branch arrived, [branchesInstances] then holding what each of them produced.
    /// <para>
    /// A join is resumed by the arrival of its last branch rather than by the state of the ones
    /// before it : several branches finishing at once would all find every other one completed, and
    /// each of them would resume the join. The arrivals are counted instead, and the instance leaves
    /// the waiting state under the lock, so a branch of the next turn through the join waits on an
    /// instance of its own.
    /// </para>
    /// </summary>
    public bool TryJoinBranches(
        BaseGraphTask node,
        TaskInstance? previousInstance,
        IReadOnlyList<BaseGraphTask> previousNodes,
        out TaskInstance instance,
        out List<TaskInstance> branchesInstances)
    {
        lock (_joinsLock)
        {
            instance = GetOrCreateWaitingInstance(node, previousInstance);
            branchesInstances = [];

            int arrived = _joinBranches.GetValueOrDefault(node.Id) + 1;

            // Still waiting for a branch to arrive, or for one of them to have produced anything.
            if (arrived < previousNodes.Count || !TryGetAllInstances(previousNodes, out branchesInstances))
            {
                _joinBranches[node.Id] = arrived;
                return false;
            }

            // Resumed : the join is claimed by this branch and starts over from no arrival, so a
            // loop leading back to it waits for its branches again.
            _joinBranches.Remove(node.Id);
            instance.State = EnumTaskState.Progressing;
            return true;
        }
    }

    private TaskInstance? GetLastInstance(Guid nodeId, EnumTaskState state = EnumTaskState.Completed)
    {
        if (NodeInstances.TryGetValue(nodeId, out var list) == false) return null;
        return list?.OrderByDescending(x => x.FinishedAt).FirstOrDefault(i => i.State == state);
    }

    public bool TryGetAllInstances(IEnumerable<BaseGraphTask> nodes, out List<TaskInstance> instances, EnumTaskState state = EnumTaskState.Completed)
    {
        instances = [];
        foreach (var node in nodes)
        {
            var previousInstance = GetLastInstance(node.Id, state);
            if (previousInstance == null)
                return false;
            instances.Add(previousInstance);
        }

        return true;
    }

    #region Handle context
    public GraphContextResult GetInputFor(BaseGraphTask node, TaskInstance? previous, JToken? sharedContext)
    {
        return GetInputFor(node, previous == null ? [] : [previous], sharedContext);
    }

    public GraphContextResult GetInputFor(BaseGraphTask node, IReadOnlyList<TaskInstance> instances, JToken? sharedContext)
    {
        GraphContextResult result = new GraphContextResult();
        if (node.InputTemplate == null)
        {
            result.Errors.Add("Node doesn't have any valid JSON template for parameters.");
            return result;
        }

        JObject inputContext = BuildContextFor(instances, sharedContext);

        var replacementResult = ReferencesHandler.ReplaceReferences(node.InputTemplate, inputContext);
        // Reference replacement errors
        if (replacementResult.HasErrors)
        {
            result.Errors.AddRange(replacementResult.Errors);
            return result;
        }

        result.Token = replacementResult.Replaced;
        return result;
    }

    /// <summary>
    /// What a node reads where the branches [instances] lead to it : "$previous", "$shared" and
    /// "$global" as its mapping names them. A node several branches reach reads them by node name,
    /// a node a single one reaches reads it directly.
    /// <para>
    /// What <see cref="GetInputFor(BaseGraphTask, IReadOnlyList{TaskInstance}, JToken?)"/> resolves
    /// the references against, exposed on its own so that an editor can show it and resolve a
    /// mapping being written the very way a run resolves it.
    /// </para>
    /// </summary>
    public JObject BuildContextFor(IReadOnlyList<TaskInstance> instances, JToken? sharedContext)
    {
        return instances.Count > 1
            ? GetMultipleContextFor(instances, sharedContext)
            : GetContextFor(instances.FirstOrDefault(), sharedContext);
    }

    private JObject GetContextFor(TaskInstance? previous, JToken? sharedContext)
    {
        return new JObject
        {
            [PreviousIdentifier] = previous?.Effective.Output,
            [SharedIdentifier] = sharedContext,
            [GlobalIdentifier] = GlobalContext,
        };
    }

    private JObject GetMultipleContextFor(IReadOnlyList<TaskInstance> instances, JToken? sharedContext)
    {
        var previouses = instances.Select(x => x.Effective).ToDictionary(x => x.NodeName, x => x.Output);
        JObject ctxt = GenerateEmptyContext();

        ctxt[GlobalIdentifier] = GlobalContext;
        ctxt[SharedIdentifier] = sharedContext;
        foreach (var pre in previouses)
        {
            JToken previousContext = ctxt[PreviousIdentifier] ?? new JObject();
            previousContext[pre.Key] = pre.Value;
            ctxt[PreviousIdentifier] ??= previousContext;
        }

        return ctxt;
    }

    private static JObject GenerateEmptyContext()
    {
        return new JObject
        {
            [PreviousIdentifier] = new JObject(),
            [SharedIdentifier] = new JObject(),
            [GlobalIdentifier] = new JObject(),
        };
    }

    /// <summary>
    /// Merge two contexts coming from two branches, the values of [other] winning. Anything that
    /// isn't an object can't be merged and is taken as-is, holding nothing being the exception : a
    /// JSON null stands for nothing to merge rather than for a value overriding what is there, the
    /// way a missing token does.
    /// </summary>
    public static JToken? MergeContexts(JToken? context, JToken? other)
    {
        if (other == null || other.Type == JTokenType.Null)
            return context;
        if (context is not JObject source || other is not JObject values)
            return other;

        JObject merged = (JObject)source.DeepClone();
        merged.Merge(values, new JsonMergeSettings
        {
            MergeArrayHandling = MergeArrayHandling.Replace,
            MergeNullValueHandling = MergeNullValueHandling.Ignore
        });
        return merged;
    }
    #endregion
}