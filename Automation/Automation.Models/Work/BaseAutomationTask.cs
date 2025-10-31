using Automation.Shared.Data;
using Newtonsoft.Json.Linq;
using NJsonSchema;
using System.Text.Json.Serialization;

namespace Automation.Models.Work
{
    [JsonDerivedType(typeof(AutomationTask), "task")]
    [JsonDerivedType(typeof(AutomationControl), "control")]
    [JsonDerivedType(typeof(AutomationWorkflow), "workflow")]
    public abstract class BaseAutomationTask : ScopedElement
    {
        [JsonIgnore]
        public JsonSchema? InputSchema
        {
            get => InputSchemaJson == null ? null : JsonSchema.FromJsonAsync(InputSchemaJson).Result;
            set => InputSchemaJson = value == null ? null : value.ToJson();
        }
        public string? InputSchemaJson { get; set; }

        [JsonIgnore]
        public JsonSchema? OutputSchema
        {
            get => OutputSchemaJson == null ? null : JsonSchema.FromJsonAsync(OutputSchemaJson).Result;
            set => OutputSchemaJson = value == null ? null : value.ToJson();
        }
        public string? OutputSchemaJson { get; set; }

        public IEnumerable<Schedule> Schedules { get; set; } = [];

        public BaseAutomationTask(EnumScopedType type) : base(type)
        { }

        public BaseAutomationTask(ScopedMetadata metadata) : base(metadata)
        { }
    }

    public class AutomationTask : BaseAutomationTask
    {
        public TaskTarget? Target { get; set; }
        public AutomationTask() : base(EnumScopedType.Task)
        { }
    }

    public class AutomationControl : AutomationTask
    {
        /// <summary>
        /// Id of the start task.
        /// </summary>
        public static readonly Guid StartTaskId = Guid.Parse("00000000-0000-0000-0000-100000000001");
        /// <summary>
        /// Id of the end task.
        /// </summary>
        public static readonly Guid EndTaskId = Guid.Parse("00000000-0000-0000-0000-100000000002");

        /// <summary>
        /// Type of the class that the target point on
        /// </summary>
        [JsonIgnore]
        public Type Type { get; set; }
        public AutomationControl(Type type)
        {
            Type = type;
        }
    }

    public class AutomationWorkflow : BaseAutomationTask
    {
        public Graph Graph { get; set; } = new Graph();

        /// <summary>
        /// Schema of all the shared data in the workflow
        /// </summary>
        [JsonIgnore]
        public JsonSchema? WorkflowSchema
        {
            get => WorkflowSchemaJson == null ? null : JsonSchema.FromJsonAsync(WorkflowSchemaJson).Result;
            set => WorkflowSchemaJson = value == null ? null : value.ToJson();
        }
        public string? WorkflowSchemaJson { get; set; }

        public AutomationWorkflow() : base(EnumScopedType.Workflow)
        {}

        /// <summary>
        /// Generate a sample of the context based on the previous tasks
        /// </summary>
        /// <param name="task"></param>
        public void GetContextSampleFor(GraphTask task)
        {
            throw new NotImplementedException();
        }
    }
}
