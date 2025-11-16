using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace Automation.Models.Work;

public class GraphExecutionContext
{
    private const string PreviousIdentifier = "previous";
    private const string GlobalIdentifier = "global";
    private const string WorkflowIdentifier = "workflow";
    private readonly Graph _graph;

    public GraphExecutionContext(Graph graph)
    {
        _graph = graph;
    }

    #region Samples

    public JObject GetBaseContext()
    {
        return new JObject
        {
            [PreviousIdentifier] = new JObject(), 
            [GlobalIdentifier] = new JObject(),
            [WorkflowIdentifier] = new JObject()
        };
    }

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
    
    /// <summary>
    /// Get the context of the task for execution.
    /// </summary>
    /// <param name="task"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void GetContextFor(BaseGraphTask task, JToken? previous, JObject context)
    {
        if (task.Settings.WaitAll)
        {
            
        }
        else
        {
            
        }
    }
}