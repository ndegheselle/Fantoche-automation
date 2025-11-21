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
    public List<JToken> GetContextSampleJsonFor(BaseGraphTask task)
    {
        var previousTasks = _graph.GetPreviousFrom(task);
        
        // TODO : get the global and common token samples
        var data = new TaskInstanceData()
        {
            
        };
        
        List<JToken> contexts = [];
        if (task.Settings.IsWaitingAllInputs)
        {
            Dictionary<string, List<JToken>> previousPotentials = new Dictionary<string, List<JToken>>();
            Dictionary<string, JToken?> previous = new Dictionary<string, JToken?>();
            foreach (var pre in previousTasks)
            {
                if (pre.Settings.IsPassingThrough)
                {
                    var preContexts = GetContextSampleJsonFor(pre);
                    previousPotentials.Add(pre.Name, preContexts);
                }
                else
                {
                    previous.Add(pre.Name, pre.OutputSchema?.ToSampleJson());
                }
            }

            foreach (var potential in previousPotentials)
            {
                foreach (var others in previousPotentials)
                {
                    if (potential.Key == others.Key)
                        continue;
                    
                }
            }
            
            previousPotentials.SelectMany(x => x.Value);
            
            contexts.Add(GenerateContextFrom(previous, data));
        }
        else
        {
            // XXX : maybe group by TaskId ?
            foreach (var previousTask in previousTasks)
            {
                if (previousTask.Settings.IsPassingThrough)
                {
                    var previousContexts = GetContextSampleJsonFor(previousTask);
                    contexts.AddRange(previousContexts);
                }
                else
                {
                    data.InputToken = previousTask.OutputSchema?.ToSampleJson();
                    contexts.Add(GenerateContextFrom(data)); 
                }
            }
        }
        return contexts;
    }

    /// <summary>
    /// Get context of all the end tasks since they act as one.
    /// </summary>
    /// <returns></returns>
    public List<JToken> GetContextSampleForEnd()
    {
        List<JToken> contexts = [];
        var endTasks = _graph.GetEndNodes();
        foreach (var task in endTasks) contexts.AddRange(GetContextSampleJsonFor(task));
        return contexts;
    }
    #endregion

    public JObject GetContextFor(BaseGraphTask task, TaskInstanceData data)
    {
        return task.Settings.IsWaitingAllInputs ? GenerateContextFrom(task.WaitedInputs, data) : GenerateContextFrom(data);
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
        JObject context = new JObject
        {
            [PreviousIdentifier] = new JObject(),
            [GlobalIdentifier] = data.GlobalToken,
            [CommonIdentifier] = data.CommonToken
        };
        
        foreach (var pre in previous)
        {
            JToken previousContext = context[PreviousIdentifier] ?? new JObject();
            previousContext[pre.Key] = pre.Value;
            context[PreviousIdentifier] ??= previousContext;
        }
        
        return context;
    }
}