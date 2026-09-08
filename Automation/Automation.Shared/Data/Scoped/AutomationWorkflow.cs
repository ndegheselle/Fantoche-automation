using Automation.Shared.Data.Graph;
using Automation.Shared.Data;
using NJsonSchema;
using System.Text.Json.Serialization;

namespace Automation.Shared.Data.Scoped
{
    public class WorkflowSettings : TaskSettings
    {
        /// <summary>
        /// Stop the whole workflow if any task fail
        /// </summary>
        public bool StopIfAnyTaskFail { get; set; } = false;
    }

    public class AutomationWorkflow : BaseAutomationTask
    {
        public TasksGraph Graph { get; set; } = new();

        public WorkflowSettings WorkflowSettings { get; set; } = new();

        private JsonSchema? _sharedSchema;

        /// <summary>
        /// The shape of the common data of the workflow, parsed once per value of
        /// <see cref="SharedSchemaJson"/> (see <see cref="Schemas.Parse"/>).
        /// </summary>
        [JsonIgnore]
        public JsonSchema? SharedSchema
        {
            get => _sharedSchema ??= Schemas.Parse(SharedSchemaJson);
            set
            {
                SharedSchemaJson = value?.ToJson();
                _sharedSchema = value;
            }
        }

        /// <summary>
        /// Schema of all the common data of the workflow.
        /// </summary>
        public string? SharedSchemaJson
        {
            get;
            set
            {
                field = value;
                _sharedSchema = null;
            }
        }

        /// <summary>
        /// Copy onto the workflow the schemas its graph declares : the start says what the workflow
        /// is started with, the end what it hands back. Those two nodes are where they are edited —
        /// they are the boundary of the graph — and a caller reads a workflow without loading its
        /// graph, so the element carries a derived copy. Called whenever the workflow is stored.
        /// </summary>
        public void DeriveSchemas()
        {
            InputSchemaJson = Graph.GetStartNodes().FirstOrDefault()?.OutputSchemaJson;
            OutputSchemaJson = Graph.GetEndNodes().FirstOrDefault()?.InputSchemaJson;
        }

        public AutomationWorkflow() : base(EnumScopedType.Workflow)
        {
        }

        public AutomationWorkflow(string name, Guid parentId) : base(new ScopedMetadata(name, EnumScopedType.Workflow))
        {
            ParentId = parentId;
        }
    }
}
