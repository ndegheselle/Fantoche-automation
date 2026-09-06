using Automation.Shared.Data.Graph;
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

        public AutomationWorkflow() : base(EnumScopedType.Workflow)
        {
        }

        public AutomationWorkflow(string name, Guid parentId) : base(new ScopedMetadata(name, EnumScopedType.Workflow))
        {
            ParentId = parentId;
        }
    }
}
