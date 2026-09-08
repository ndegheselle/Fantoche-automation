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

        if (node.AutomationTask is AutomationWorkflow workflow)
            instance = new WorkflowInstance(workflow);
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
    /// Return true if all previous branches are completed, false overwise. [branchesInstances] contains the list of previous instances if true.
    /// This check is locked since async branches may race to free the same join node.
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
            return TryGetAllInstances(previousNodes, out branchesInstances);
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

        JObject inputContext = instances.Count > 1 ?
            GetMultipleContextFor(node, instances, sharedContext) :
            GetContextFor(node, instances.FirstOrDefault(), sharedContext);

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

    private JObject GetContextFor(BaseGraphTask task, TaskInstance? previous, JToken? sharedContext)
    {
        return new JObject
        {
            [PreviousIdentifier] = previous?.Effective.Output,
            [SharedIdentifier] = sharedContext,
            [GlobalIdentifier] = GlobalContext,
        };
    }

    private JObject GetMultipleContextFor(BaseGraphTask task, IReadOnlyList<TaskInstance> instances, JToken? sharedContext)
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
    /// isn't an object can't be merged and is taken as-is.
    /// </summary>
    public static JToken? MergeContexts(JToken? context, JToken? other)
    {
        if (other == null)
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