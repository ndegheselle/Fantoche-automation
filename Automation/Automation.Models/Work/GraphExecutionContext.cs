using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace Automation.Models.Work
{
    public class GraphExecutionContext
    {
        private const string PreviousIdentifier = "previous";
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
        public List<string> GetContextSchemaFor(BaseGraphTask task)
        {
            var previousTasks = _graph.GetPreviousFrom(task);
            
            List<string> contexts = [];
            if (task.Settings.WaitAll)
            {
                var context = new JObject();
                var previous = new JObject();
                context[PreviousIdentifier] = previous;
                foreach (var previousTask in previousTasks)
                {
                    previous[previousTask.Name] = previousTask.OutputSchemaJson;
                }
                contexts.Add(context.ToString());
            }
            else
            {
                // XXX : maybe group by TaskId ?
                foreach (var previousTask in previousTasks)
                {
                    var context = new JObject
                    {
                        [PreviousIdentifier] = previousTask.OutputSchemaJson
                    };
                    contexts.Add(context.ToString());
                }
            }
            return contexts;
        }

        /// <summary>
        /// Get the context of the task for execution.
        /// </summary>
        /// <param name="task"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void GetContextFor(BaseGraphTask task)
        {
            throw new NotImplementedException("Add instance of the previous task in parameter");
        }
        
        /// <summary>
        /// Get the output schema for the current graph.
        /// </summary>
        public void GetOutputSchemaFor()
        {
            throw new NotImplementedException("Join all the end control task previous Schema (handle previous task as well)");
        }
        
        #endregion
    }
}