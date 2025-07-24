using Automation.Shared.Data;
using System.Text.Json.Serialization;

namespace Automation.Dal.Models
{
    public partial class TaskConnector
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public EnumTaskConnectorType Type { get; set; }
        public EnumTaskConnectorDirection Direction { get; set; }
    }

    [JsonDerivedType(typeof(AutomationWorkflow), "workflow")]
    [JsonDerivedType(typeof(AutomationTask), "task")]
    public abstract class BaseAutomationTask : ScopedElement
    {
        public IEnumerable<TaskConnector> Inputs { get; set; } = [];
        public IEnumerable<TaskConnector> Outputs { get; set; } = [];

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
        public Type Type { get; set; }
        public AutomationControl(Type type)
        {
            Type = type;
        }
    }

    public class AutomationWorkflow : BaseAutomationTask
    {
        public Graph Graph { get; set; }
        public AutomationWorkflow(Graph graph) : base(EnumScopedType.Workflow)
        {
            Graph = graph;
        }
    }
}
