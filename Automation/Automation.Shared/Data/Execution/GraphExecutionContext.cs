using Automation.Shared.Data.Graph;
using Newtonsoft.Json.Linq;

namespace Automation.Shared.Data.Execution;

public class GraphContextResolutionException : Exception
{
    public GraphContextResolutionException(string message) : base(message)
    { }
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
    public JObject GetInstanceContextFor(BaseGraphTask task, TaskInstance previousInstance)
    {
        return GenerateContextFrom(ResolveEffectiveInstance(previousInstance).Output, _workflowInstance.SharedContext, _workflowInstance.GlobalContext);
    }

    public JObject GetWaitedInstanceContextFor(BaseGraphTask task, IReadOnlyList<TaskInstance> instances)
    {
        var previouses = instances.Select(ResolveEffectiveInstance).ToDictionary(x => x.NodeName, x => x.Output);
        return GenerateContextFrom(previouses, _workflowInstance.SharedContext, _workflowInstance.GlobalContext);
    }
    #endregion

    #region Get sample from task
    public IEnumerable<JObject> GetSamplesFor(BaseGraphTask task)
        => GetSamplesFor(_graph, task, _workflowInstance.SharedContext, _workflowInstance.GlobalContext);

    public JObject GetWaitedSamplesFor(BaseGraphTask task)
        => GetWaitedSamplesFor(_graph, task, _workflowInstance.SharedContext, _workflowInstance.GlobalContext);

    /// <summary>
    /// One context per branch reaching [task], each holding a sample of what the branch produces
    /// rather than what it produced : the graph as it would run, known without running it.
    /// </summary>
    public static IEnumerable<JObject> GetSamplesFor(TasksGraph graph, BaseGraphTask task, JToken? shared, JToken? global)
    {
        var previouses = graph.GetPrevious(task).SelectMany(x => ResolveEffectiveTask(graph, x));
        return previouses.Select(x => GenerateContextFrom(x.OutputSchema?.ToSampleJson(), shared, global));
    }

    /// <summary>
    /// The single context of a task waiting for every branch reaching it, each sample being held
    /// under the name of the node producing it.
    /// </summary>
    public static JObject GetWaitedSamplesFor(TasksGraph graph, BaseGraphTask task, JToken? shared, JToken? global)
    {
        var previouses = graph.GetPrevious(task).SelectMany(x => ResolveEffectiveTask(graph, x));
        return GenerateContextFrom(previouses.ToDictionary(x => x.Name, x => x.OutputSchema?.ToSampleJson()), shared, global);
    }
    #endregion

    #region Resolve
    /// <summary>
    /// What a node actually stands for : a node passing through produces nothing of its own, the
    /// branches leading to it are what the next ones read.
    /// </summary>
    private static IEnumerable<BaseGraphTask> ResolveEffectiveTask(TasksGraph graph, BaseGraphTask task)
    {
        if (task.AutomationTask?.Settings.IsPassingThrough != true)
            return [task];
        return graph.GetPrevious(task).SelectMany(x => ResolveEffectiveTask(graph, x));
    }

    private TaskInstance ResolveEffectiveInstance(TaskInstance instance)
    {
        if (instance.Node?.AutomationTask?.Settings.IsPassingThrough != true)
            return instance;
        if (instance.Previous == null)
            throw new GraphContextResolutionException("Could not resolve an effective task instance.");
        return ResolveEffectiveInstance(instance.Previous);
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