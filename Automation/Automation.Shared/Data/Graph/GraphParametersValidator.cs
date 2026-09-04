using Automation.Shared.Data.Execution;
using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace Automation.Shared.Data.Graph
{
    /// <summary>
    /// Check the parameters template of a node without running anything : its references are
    /// resolved against samples of what it reads (the output schema of the branches leading to it,
    /// the shared values and the context of the scopes), and what comes out of the resolution is
    /// validated against the schema the node is expected to produce.
    /// </summary>
    public static class GraphParametersValidator
    {
        /// <summary>
        /// What is wrong with [inputMappingJson] for [node], empty when the template holds up. Each
        /// branch reaching the node is checked on its own : the parameters have to be valid whichever
        /// one leads to it.
        /// </summary>
        /// <param name="expected">
        /// Schema the resolved parameters have to match, null when the node isn't expecting a shape
        /// (the references are then the only thing checked). An empty template is checked against it
        /// too : a task expecting something isn't given anything.
        /// </param>
        public static List<string> Validate(
            TasksGraph graph,
            BaseGraphTask node,
            string? inputMappingJson,
            JsonSchema? expected,
            JToken? shared,
            JToken? global)
        {
            if (string.IsNullOrWhiteSpace(inputMappingJson))
            {
                // Nothing is handed over at runtime : an empty template only holds up when the task
                // is fine with no value at all.
                if (expected == null)
                    return [];

                var missing = expected.Validate(JValue.CreateNull());
                if (missing.Count == 0)
                    return [];

                return [$"No parameters while the task expects some ({string.Join(", ", missing.Select(x => x.Kind).Distinct())})."];
            }

            JToken template;
            try
            {
                template = JToken.Parse(inputMappingJson);
            }
            catch
            {
                // Not JSON yet : what it is made of is checked once it parses.
                return [];
            }

            // A join reads every branch at once, the others one branch at a time. A node with
            // nothing before it still reads the shared values and the context of its scopes.
            List<JObject> contexts = node is GraphControl control && control.IsJoin()
                ? [GraphExecutionContext.GetWaitedSamplesFor(graph, node, shared, global)]
                : [.. GraphExecutionContext.GetSamplesFor(graph, node, shared, global)];
            if (contexts.Count == 0)
                contexts = [GraphExecutionContext.GenerateContextFrom((JToken?)null, shared, global)];

            List<string> errors = [];
            foreach (JObject context in contexts)
            {
                // Resolved on a copy : the replacement writes the values into the template it walks.
                ReferenceReplaceContext resolution = ReferencesHandler.ReplaceReferences(template.DeepClone(), context);
                foreach (ReferenceReplaceError error in resolution.Errors)
                    errors.Add(error.ToString());

                if (expected == null)
                    continue;

                foreach (var error in expected.Validate(resolution.ReplacedSetting))
                    errors.Add($"{error.Path} : {error.Kind}");
            }

            // The same mistake is reported once, whatever the number of branches it shows up in.
            return [.. errors.Distinct()];
        }
    }
}
