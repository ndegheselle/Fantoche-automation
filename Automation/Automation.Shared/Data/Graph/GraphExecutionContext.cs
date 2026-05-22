using Automation.Shared.Data.Execution;
using Newtonsoft.Json.Linq;

namespace Automation.Shared.Data.Graph;

public class GraphExecutionContext
{
    private const string PreviousIdentifier = "previous";
    private const string ContextIdentifier = "context";
    private readonly TasksGraph _graph;

    public GraphExecutionContext(TasksGraph graph)
    {
        _graph = graph;
    }

    #region Samples

    /// <summary>
    /// Generate a sample of the contexts based on the previous tasks.
    /// </summary>
    /// <param name="task"></param>
    public List<string> GetContextSampleJsonFor(BaseGraphTask task)
    {
        var previousTasks = _graph.GetPreviousFrom(task);

        // TODO : get the global and common token samples
        JToken? context = null;

        List<string> contexts = [];
        if (task.Settings.IsWaitingAllInputs)
        {
            Dictionary<string, JToken?> previous = new Dictionary<string, JToken?>();
            foreach (var pre in previousTasks)
                previous.Add(pre.Name, pre.OutputSchema?.ToSampleJson());
            contexts.Add(GenerateContextFrom(previous, context).ToString());
        }
        else
        {
            // XXX : maybe group by TaskId ?
            foreach (var previousTask in previousTasks)
            {
                var input = previousTask.OutputSchema?.ToSampleJson();
                contexts.Add(GenerateContextFrom(input, context).ToString());
            }
        }
        return contexts;
    }

    /// <summary>
    /// Get context of all the end tasks since they act as one.
    /// </summary>
    /// <returns></returns>
    public List<string> GetContextSampleForEnd()
    {
        List<string> contexts = [];
        var endTasks = _graph.GetEndNodes();
        foreach (var task in endTasks) contexts.AddRange(GetContextSampleJsonFor(task));
        return contexts;
    }
    #endregion

    /// <summary>
    /// Build the context for a task from its previous instances.
    /// If the task waits for all inputs the context is keyed by previous node name,
    /// otherwise the single previous output is used as-is.
    /// </summary>
    public JObject GetContextFor(BaseGraphTask task, IReadOnlyList<NodeInstance> previousInstances, JToken? context)
    {
        if (previousInstances.Count == 0 && context == null)
            return GenerateEmptyContext();

        if (task.Settings.IsWaitingAllInputs)
        {
            var byName = new Dictionary<string, JToken?>();
            foreach (var instance in previousInstances)
                byName[instance.Name] = instance.Output;
            return GenerateContextFrom(byName, context);
        }

        var single = previousInstances.Count > 0 ? previousInstances[0].Output : null;
        return GenerateContextFrom(single, context);
    }

    public JObject GenerateEmptyContext()
    {
        return new JObject
        {
            [PreviousIdentifier] = new JObject(),
            [ContextIdentifier] = new JObject()
        };
    }

    public JObject GenerateContextFrom(JToken? previous, JToken? context)
    {
        return new JObject
        {
            [PreviousIdentifier] = previous,
            [ContextIdentifier] = context,
        };
    }

    public JObject GenerateContextFrom(Dictionary<string, JToken?> previous, JToken? context)
    {
        JObject ctxt = GenerateEmptyContext();

        ctxt[ContextIdentifier] = context;
        foreach (var pre in previous)
        {
            JToken previousContext = ctxt[PreviousIdentifier] ?? new JObject();
            previousContext[pre.Key] = pre.Value;
            ctxt[PreviousIdentifier] ??= previousContext;
        }

        return ctxt;
    }
}