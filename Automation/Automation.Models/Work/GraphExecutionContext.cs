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
        public string? GetContextSampleFor(BaseGraphTask task)
        {
            var previousTasks = _graph.GetPreviousFrom(task);
            
            string? context = null;
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
            throw new NotImplementedException("Add instance of the previous task in parameter");
        }
        
        /// <summary>
        /// Get the output schema for the current graph.
        /// </summary>
        public void GetOutputSchemaFor()
        {
            throw new NotImplementedException("Join all the end control task previous Schema (handle previous task as well)");
        }
        
        private JsonSchema  MergeSchemas(params JsonSchema[] schemas)
        {
            if (schemas == null || schemas.Length == 0)
                return null;

            // Start with the first schema as the base
            var mergedSchema = schemas[0];

            for (int i = 1; i < schemas.Length; i++)
            {
                var currentSchema = schemas[i];
                if (currentSchema == null)
                    continue;

                // Merge properties
                foreach (var property in currentSchema.Properties)
                {
                    if (mergedSchema.Properties.TryGetValue(property.Key, out var existingProperty))
                    {
                        // If both properties are objects, recursively merge them
                        if (existingProperty.Type == JsonObjectType.Object &&
                            property.Value.Type == JsonObjectType.Object)
                        {
                            var nestedMergedSchema = MergeSchemas(existingProperty, property.Value);
                            mergedSchema.Properties[property.Key] = nestedMergedSchema;
                        }
                        else
                        {
                            // Otherwise, use AnyOf to combine them
                            mergedSchema.Properties[property.Key] = new JsonSchema
                            {
                                AnyOf = { existingProperty, property.Value }
                            };
                        }
                    }
                    else
                    {
                        // Otherwise, add the property
                        mergedSchema.Properties.Add(property.Key, property.Value);
                    }
                }
            }

            return mergedSchema;
        }
        
        #endregion
    }
}