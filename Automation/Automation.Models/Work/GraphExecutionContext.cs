using Newtonsoft.Json.Linq;

namespace Automation.Models.Work;

public class GraphExecutionContext
{
    private const string PreviousIdentifier = "previous";
    private const string GlobalIdentifier = "global";
    private const string CommonIdentifier = "common";
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
        var data = new TaskInstanceData()
        {
            
        };
        
        List<string> contexts = [];
        if (task.Settings.IsWaitingAllInputs)
        {
            Dictionary<string, JToken?> previous = new Dictionary<string, JToken?>();
            foreach (var pre in previousTasks)
                previous.Add(pre.Name, pre.OutputSchema?.ToSampleJson());
            contexts.Add(GenerateContextFrom(previous, data).ToString());
        }
        else
        {
            // XXX : maybe group by TaskId ?
            foreach (var previousTask in previousTasks)
            {
                data.InputToken = previousTask.OutputSchema?.ToSampleJson();
                contexts.Add(GenerateContextFrom(data).ToString());
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

    public JObject GetContextFor(BaseGraphTask task, TaskInstanceData? data)
    {
        if (data == null)
            return GenerateEmptyContext();
        return task.Settings.IsWaitingAllInputs ? GenerateContextFrom(task.WaitedInputs, data) : GenerateContextFrom(data);
    }
    
    public JObject GenerateEmptyContext()
    {
        return new JObject
        {
            [PreviousIdentifier] = null,
            [GlobalIdentifier] = null,
            [CommonIdentifier] = null
        };
    }
    
    public JObject GenerateContextFrom(TaskInstanceData data)
    {
        return new JObject
        {
            [PreviousIdentifier] = data.InputToken,
            [GlobalIdentifier] = data.GlobalToken,
            [CommonIdentifier] = data.CommonToken
        };
    }

    public JObject GenerateContextFrom(Dictionary<string, JToken?> previous, TaskInstanceData data)
    {
        JObject context = GenerateContextFrom(data);

        context[PreviousIdentifier] = new JObject();
        foreach (var pre in previous)
        {
            JToken previousContext = context[PreviousIdentifier] ?? new JObject();
            previousContext[pre.Key] = pre.Value;
            context[PreviousIdentifier] ??= previousContext;
        }
        
        return context;
    }
}