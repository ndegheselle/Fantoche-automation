using System.Text.Json.Serialization;
using Automation.Shared.Data.Graph;
using NJsonSchema;

namespace Automation.Shared.Data.Scoped
{
    public class WorkflowSettings : TaskSettings
    {
        /// <summary>
        /// Store all data (input, output, global, ...) in task instance. Can be used to track data change precisely but data will be duplicated for each node.
        /// </summary>
        public bool IsStoringAllData { get; set; } = false;

        /// <summary>
        /// If there is multiple end nodes, stop at the first one encoutered (and kill all unfinished tasks).
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

        /// <summary>
        /// Mapping for the output of the workflow.
        /// </summary>
        public string? OutputMappingJson { get; set; }

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
