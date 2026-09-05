using System.Text.Json.Serialization;
using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Graph;
using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace Automation.Shared.Data.Scoped
{
    public class WorkflowSettings : TaskSettings
    {
        /// <summary>
        /// Stop as soon as one branch reaches the end, instead of waiting for every branch reaching
        /// it (and kill all unfinished tasks). What the end reads changes with it : a single
        /// "$previous" when it stops at the first branch, one entry per branch otherwise.
        /// </summary>
        public bool StopAtFirstEnd { get; set; } = false;

        /// <summary>
        /// Stop the whole workflow if any task fail
        /// </summary>
        public bool StopIfAnyTaskFail { get; set; } = false;
    }

    public class AutomationWorkflow : BaseAutomationTask
    {
        public TasksGraph Graph { get; set; } = new();

        public WorkflowSettings WorkflowSettings { get; set; } = new();

        [JsonIgnore]
        public JsonSchema? SharedSchema
        {
            get => SharedSchemaJson == null ? null : JsonSchema.FromJsonAsync(SharedSchemaJson).Result;
            set => SharedSchemaJson = value == null ? null : value.ToJson();
        }

        /// <summary>
        /// Schema of all the common data of the workflow.
        /// </summary>
        public string? SharedSchemaJson { get; set; }

        /// <summary>
        /// The values the start hands over for what the caller doesn't give. Read from the mapping
        /// of the start, references included : a default can point at the context of the scopes
        /// ("$global"), there being nothing else to point at before the workflow runs.
        /// </summary>
        /// <exception cref="GraphContextResolutionException">
        /// The mapping of the start isn't JSON, so what it stands for can't be read.
        /// </exception>
        public JToken? GetInputDefaults(JToken? global = null)
        {
            GraphControl? start = Graph.GetStartNodes().FirstOrDefault();
            if (start == null)
                return null;

            if (!start.IsInputMappingValid)
                throw new GraphContextResolutionException($"The default values of '{Metadata.Name}' are not valid JSON.");

            return start.ResolveInputMapping(GraphContext.From(null, null, null, global));
        }

        /// <summary>
        /// [parameters] completed by the default values of the start : what the workflow actually
        /// runs with. Applied before the input is validated, the defaults being part of it.
        /// </summary>
        public JToken? ApplyInputDefaults(JToken? parameters, JToken? global = null)
            => GraphContext.ApplyDefaults(GetInputDefaults(global), parameters);

        /// <summary>
        /// The graph as it would run : what every node reads and hands over, without running it.
        /// One per edition or per save, it keeps what it computes (see <see cref="GraphSampling"/>).
        /// </summary>
        /// <param name="global">
        /// Context of the scopes holding the workflow, what a "$global" reference points at. Read
        /// from the scoped service, so it is handed over rather than looked up.
        /// </param>
        public GraphSampling Sample(JToken? global = null) => new(this, global);

        /// <summary>
        /// Write the schemas the graph deduces from its mappings and return what makes it invalid.
        /// Called when the workflow is stored : the schemas are read by whoever uses it without
        /// loading its graph, so they follow the mappings rather than being edited on their own.
        /// </summary>
        public List<string> DeriveSchemas() => Sample().DeriveSchemas();

        public AutomationWorkflow() : base(EnumScopedType.Workflow)
        {
        }

        public AutomationWorkflow(string name, Guid parentId) : base(new ScopedMetadata(name, EnumScopedType.Workflow))
        {
            ParentId = parentId;
        }
    }
}
