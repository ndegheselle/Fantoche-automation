using Newtonsoft.Json.Linq;

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
        public List<string?> GetContextSampleFor(BaseGraphTask task)
        {
            var previousTasks = _graph.GetPreviousFrom(task);
            
            List<string?> contexts = [];
            if (task.Settings.WaitAll)
            {
                var context = new JObject();
                var previous = new JObject();
                context[PreviousIdentifier] = previous;
                foreach (BaseGraphTask previousTask in previousTasks)
                {
                    previous[previousTask.Name] = previousTask.OutputSchema?.ToSampleJson();
                }
                contexts.Add(context.ToString());
            }
            else
            {
                // XXX : maybe group by TaskId ?
                foreach (BaseGraphTask previousTask in previousTasks)
                {
                    var context = new JObject
                    {
                        [PreviousIdentifier] = previousTask.OutputSchema?.ToSampleJson()
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
            // TODO : add instance of the previous task in parameter
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// Get the output schema for the current graph.
        /// </summary>
        public void GetOutputSchemaFor()
        {
            // TODO : join all the end control task previous Schema (handle previous task as well)
            throw new NotImplementedException();
        }
        
        #endregion
    }
}