using Automation.Shared.Data;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace Automation.Dal.Models
{
    public class TaskConnectors : IAutomationTaskConnector
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public EnumTaskConnectorType Type { get; set; }

        public EnumTaskConnectorDirection Direction { get; set; }
    }

    [BsonKnownTypes(typeof(AutomationWorkflow)), JsonDerivedType(typeof(AutomationWorkflow), "workflow")]
    [BsonKnownTypes(typeof(AutomationTask)), JsonDerivedType(typeof(AutomationTask), "task")]
    public abstract class BaseAutomationTask : ScopedElement
    {
        public IEnumerable<IAutomationTaskConnector> Inputs { get; set; } = [];

        public IEnumerable<IAutomationTaskConnector> Outputs { get; set; } = [];

        public IEnumerable<Schedule> Schedules { get; set; } = [];

        public BaseAutomationTask(EnumScopedType type) : base(type)
        { }
    }

    public class AutomationTask : BaseAutomationTask, IAutomationTask
    {
        [BsonIgnore]
        ITaskTarget? IAutomationTask.Target => Target;
        public TaskTarget? Target { get; set; }
        public AutomationTask() : base(EnumScopedType.Task)
        { }
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
