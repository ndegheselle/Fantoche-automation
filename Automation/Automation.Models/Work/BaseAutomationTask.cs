using Automation.Shared.Data;
using NJsonSchema;
using System.Text.Json.Serialization;

namespace Automation.Models.Work
{
    public partial class TaskConnector
    {
        public Guid Id { get; set; }
        public JsonSchema Schema { get; set; }
        public string SchemaJson
        {
            get => Schema.ToJson(Newtonsoft.Json.Formatting.Indented);
            set => Schema = JsonSchema.FromJsonAsync(value).Result;
        }

        public TaskConnector(JsonSchema schema)
        {
            Id = Guid.NewGuid();
            Schema=schema;
        }
    }

    [JsonDerivedType(typeof(AutomationTask), "task")]
    [JsonDerivedType(typeof(AutomationControl), "control")]
    [JsonDerivedType(typeof(AutomationWorkflow), "workflow")]
    public abstract class BaseAutomationTask : ScopedElement
    {
        public IEnumerable<TaskConnector> Inputs { get; set; } = [new TaskConnector(new JsonSchema())];
        public IEnumerable<TaskConnector> Outputs { get; set; } = [new TaskConnector(new JsonSchema())];

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

        public void UpdateConnectors(JsonSchema input, JsonSchema output)
        {
            Inputs = [new TaskConnector(input)];
            Outputs = [new TaskConnector(output)];
        }
    }

    public class AutomationControl : AutomationTask
    {
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
        public AutomationWorkflow() : base(EnumScopedType.Workflow)
        {}
    }
}
