using Automation.Dal.Models.Schema;
using Automation.Shared.Data;
using System.Text.Json.Serialization;

namespace Automation.Dal.Models
{
    // XXX : may be replaced by a simple guid if there is no need for more information
    public partial class TaskConnector
    {
        public Guid Id { get; set; }
        public SchemaProperty Property { get; set; }
    }

    [JsonDerivedType(typeof(AutomationTask), "task")]
    [JsonDerivedType(typeof(AutomationControl), "control")]
    [JsonDerivedType(typeof(AutomationWorkflow), "workflow")]
    public abstract class BaseAutomationTask : ScopedElement
    {
        public IEnumerable<TaskConnector> Inputs { get; set; } = [new TaskConnector()];
        public IEnumerable<TaskConnector> Outputs { get; set; } = [new TaskConnector()];

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
