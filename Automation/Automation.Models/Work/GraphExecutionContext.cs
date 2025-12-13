using Newtonsoft.Json.Linq;

namespace Automation.Models.Work;

public class GraphExecutionContext
{
    private const string PreviousIdentifier = "previous";
    private const string ContextIdentifier = "context";
    private readonly Graph _graph;

    public GraphExecutionContext(Graph graph)
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

    public JObject GetContextFor(BaseGraphTask task, JToken? previous, JToken? context)
    {
        if (previous == null && context == null)
            return GenerateEmptyContext();
        return task.Settings.IsWaitingAllInputs ? GenerateContextFrom(task.WaitedInputs, context) : GenerateContextFrom(previous, context);
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