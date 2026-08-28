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
    {
        var previouses = _graph.GetPrevious(task).SelectMany(ResolveEffectiveTask);
        return previouses.Select(x => GenerateContextFrom(x.OutputSchema?.ToSampleJson(), _workflowInstance.SharedContext, _workflowInstance.GlobalContext));
    }

    public JObject GetWaitedSamplesFor(BaseGraphTask task)
    {
        var previouses = _graph.GetPrevious(task).SelectMany(ResolveEffectiveTask);
        return GenerateContextFrom(previouses.ToDictionary(x => x.Name, x => x.OutputSchema?.ToSampleJson()), _workflowInstance.SharedContext, _workflowInstance.GlobalContext);
    }
    #endregion

    #region Resolve
    private IEnumerable<BaseGraphTask> ResolveEffectiveTask(BaseGraphTask task)
    {
        if (task.AutomationTask?.Settings.IsPassingThrough != true)
            return [task];
        return _graph.GetPrevious(task).SelectMany(ResolveEffectiveTask);
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
    public JObject GenerateContextFrom(JToken? previous, JToken? shared, JToken? global)
    {
        return new JObject
        {
            [PreviousIdentifier] = previous,
            [SharedIdentifier] = shared,
            [GlobalIdentifier] = global,
        };
    }

    public JObject GenerateEmptyContext()
    {
        return new JObject
        {
            [PreviousIdentifier] = new JObject(),
            [SharedIdentifier] = new JObject(),
            [GlobalIdentifier] = new JObject(),
        };
    }

    public JObject GenerateContextFrom(Dictionary<string, JToken?> previous, JToken? shared, JToken? global)
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