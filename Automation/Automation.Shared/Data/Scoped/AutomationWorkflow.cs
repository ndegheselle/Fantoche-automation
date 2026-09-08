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

        public AutomationWorkflow() : base(EnumScopedType.Workflow)
        {
        }

        public AutomationWorkflow(string name, Guid parentId) : base(new ScopedMetadata(name, EnumScopedType.Workflow))
        {
            ParentId = parentId;
        }
    }
}
