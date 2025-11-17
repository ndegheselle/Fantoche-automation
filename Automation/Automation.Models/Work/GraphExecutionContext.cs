using Newtonsoft.Json.Linq;
using NJsonSchema;

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

        List<string> contexts = [];
        if (task.Settings.WaitAll)
        {
            var context = GetBaseContext();
            foreach (var previousTask in previousTasks)
            {
                var previousTaskContext = previousTask.OutputSchema?.ToSampleJson();
                if (previousTaskContext == null) continue;

                var previousContext = context[PreviousIdentifier] ?? new JObject();
                previousContext[previousTask.Name] = previousTaskContext;
                context[PreviousIdentifier] ??= previousContext;
            }

            contexts.Add(context.ToString());
        }
        else
        {
            // XXX : maybe group by TaskId ?
            foreach (var previousTask in previousTasks)
            {
                var previousContext = previousTask.OutputSchema?.ToSampleJson();
                var context = GetBaseContext();
                if (previousContext != null)
                    context[PreviousIdentifier] = previousContext;
                contexts.Add(context.ToString());
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
    
    public JObject GetContextFor(JToken? previous, TaskInstanceData data)
    {
        return new JObject
        {
            [PreviousIdentifier] = previous,
            [GlobalIdentifier] = data.GlobalToken,
            [CommonIdentifier] = data.CommonToken
        };
    }

    public JObject GetContextFor(Dictionary<string, JToken?> previous, TaskInstanceData data)
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