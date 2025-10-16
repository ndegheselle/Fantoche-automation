using Automation.Shared.Data;
using NJsonSchema;
using System.Text.Json.Serialization;

namespace Automation.Models.Work
{
    [JsonDerivedType(typeof(AutomationTask), "task")]
    [JsonDerivedType(typeof(AutomationControl), "control")]
    [JsonDerivedType(typeof(AutomationWorkflow), "workflow")]
    public abstract class BaseAutomationTask : ScopedElement
    {
        public JsonSchema? InputSchema { get; set; }
        public JsonSchema? OutputSchema { get; set; }

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
