using Automation.Shared.Data.Graph;
using Automation.Shared.Data.Scoped;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Automation.Shared.Data.Execution;

public class GraphContextResolutionException : Exception
{
    public GraphContextResolutionException(string message) : base(message)
    { }
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

    public Guid WorkflowInstanceId { get; set; } = null;

    public JToken? GlobalContext { get; set; }
    public ConcurrentDictionary<Guid, List<TaskInstance>> NodeInstances { get; } = [];

    public TaskInstance CreateInstance(BaseGraphTask node, JToken? parameters, EnumTaskState state = EnumTaskState.Pending, TaskInstance? previous = null)
    {
        TaskInstance instance;

        if (node.AutomationTask is AutomationWorkflow workflow)
            instance = new WorkflowInstance(workflow);
        else
            instance = new TaskInstance();

        instance.ParentInstanceId = WorkflowInstanceId;
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

    public TaskInstance? GetLastInstance(BaseGraphTask node)
    {
        NodeInstances.TryGetValue(node.Id, out var list);
        return list?.OrderByDescending(x => x.FinishedAt).FirstOrDefault(i => i.State == EnumTaskState.Completed);
    }

    #region Handle context
    public GraphContextResult GetInputFor(BaseGraphTask node, TaskInstance? previous, JToken? sharedContext)
    {
        GraphContextResult result = new GraphContextResult();
        if (node.InputMapping == null)
        {
            result.Errors.Add("Node doesn't have any valid JSON parameters");
            return result;
        }

        var inputContext = GetContextFor(node, previous, sharedContext);
        var replacementResult = ReferencesHandler.ReplaceReferences(node.InputMapping, inputContext);
        // Referencence replacement error
        if (replacementResult.HasErrors)
        {
            result.Errors.AddRange(replacementResult.Errors);
            return result;
        }
        return result;
    }

    private JObject GetContextFor(BaseGraphTask task, TaskInstance? previousInstance, JToken? sharedContext)
    {
        return new JObject
        {
            [PreviousIdentifier] = previousInstance?.Effective.Output,
            [SharedIdentifier] = sharedContext,
            [GlobalIdentifier] = GlobalContext,
        };
    }

    private JObject GetWaitedContextFor(BaseGraphTask task, IReadOnlyList<TaskInstance> instances, JToken? sharedContext)
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

public class GraphExecutionPreview
{
    public Dictionary<Guid, List<string>> NodesErrors { get; } = [];

    // XXX : graph need to be refreshed
    public void BuildSamples(TasksGraph graph, GraphContextResolution resolution)
    {
        foreach (var start in graph.GetStartNodes())
        {
            // Merge schema sample with default values
            var startSample = GraphContextResolution.MergeContexts(start.OutputSchema?.ToSampleJson(), start.InputMapping);
            var instance = resolution.CreateInstance(start, startSample, EnumTaskState.Completed);

            foreach(var nextNode in graph.GetNext(start))
                Next(graph, nextNode.Task, instance, new JObject(), resolution);
        }
    }

    private void Next(TasksGraph graph, BaseGraphTask node, TaskInstance? previous, JToken? sharedContext, GraphContextResolution resolution)
    {
        // We pass on each node exactly once
        if (resolution.NodeInstances.ContainsKey(node.Id))
            return;

        // XXX : I don't even need the input ?
        var result = resolution.GetInputFor(node, previous, sharedContext);
        if (result.HasError)
        {
            NodesErrors.Add(node.Id, result.Errors);
            return;
        }

        var instance = resolution.CreateInstance(node, result.Token, EnumTaskState.Completed, previous);
        instance.Output = node.OutputSchema?.ToSampleJson();

        foreach (var nextNode in graph.GetNext(node))
            Next(graph, nextNode.Task, instance, sharedContext, resolution);
    }

    private void BuildNodeSample()
    {

    }
}

public class GraphExecutionContext
{
    private const string PreviousIdentifier = "previous";
    private const string SharedIdentifier = "shared";
    private const string GlobalIdentifier = "global";

    private readonly TasksGraph _graph;
    private readonly WorkflowInstance _workflowInstance;

    public GraphExecutionContext(TasksGraph graph, WorkflowInstance workflowInstance)
    {
        _graph = graph;
        _workflowInstance = workflowInstance;
    }

    #region Get from instances
    public JObject GetContextFor(BaseGraphTask task, TaskInstance previousInstance)
    {
        return GenerateContextFrom(ResolveEffective(previousInstance).Output, _workflowInstance.SharedContext, _workflowInstance.GlobalContext);
    }

    public JObject GetWaitedContextFor(BaseGraphTask task, IReadOnlyList<TaskInstance> instances)
    {
        var previouses = instances.Select(ResolveEffective).ToDictionary(x => x.NodeName, x => x.Output);
        return GenerateContextFrom(previouses, _workflowInstance.SharedContext, _workflowInstance.GlobalContext);
    }
    #endregion

    #region Generate
    public static JObject GenerateContextFrom(JToken? previous, JToken? shared, JToken? global)
    {
        return new JObject
        {
            [PreviousIdentifier] = previous,
            [SharedIdentifier] = shared,
            [GlobalIdentifier] = global,
        };
    }

    public static JObject GenerateEmptyContext()
    {
        return new JObject
        {
            [PreviousIdentifier] = new JObject(),
            [SharedIdentifier] = new JObject(),
            [GlobalIdentifier] = new JObject(),
        };
    }

    public static JObject GenerateContextFrom(Dictionary<string, JToken?> previous, JToken? shared, JToken? global)
    {
        JObject ctxt = GenerateEmptyContext();

        ctxt[GlobalIdentifier] = global;
        ctxt[SharedIdentifier] = shared;
        foreach (var pre in previous)
        {
            JToken previousContext = ctxt[PreviousIdentifier] ?? new JObject();
            previousContext[pre.Key] = pre.Value;
            ctxt[PreviousIdentifier] ??= previousContext;
        }

        return ctxt;
    }

    #endregion
}