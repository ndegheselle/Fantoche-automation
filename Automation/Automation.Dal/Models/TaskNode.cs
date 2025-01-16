using Automation.Shared.Data;
using MongoDB.Bson.Serialization.Attributes;

namespace Automation.Dal.Models
{
    [BsonKnownTypes(typeof(WorkflowNode))]
    public class TaskNode : ScopedElement, ITaskNode
    {
        public IEnumerable<ITaskConnector> Inputs { get; set; } = [];
        public IEnumerable<ITaskConnector> Outputs { get; set; } = [];
        public IEnumerable<Schedule> Schedules { get; set; } = [];
        public TargetedPackage? Package { get; set; }

        public TaskNode() {
            Type = EnumScopedType.Task;
        }
    }

    public class TaskConnectors : ITaskConnector
    {
        public Guid Id { get; set; }
        public string Name {get;set;} = string.Empty;
        public EnumTaskConnectorType Type {get;set;}
        public EnumTaskConnectorDirection Direction {get;set;}
    }
}
