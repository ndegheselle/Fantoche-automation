using Automation.Shared.Data;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace Automation.Dal.Models
{
    [JsonDerivedType(typeof(WorkflowNode), "workflow")]
    [BsonKnownTypes(typeof(WorkflowNode))]
    public class TaskNode : ScopedElement, IAutomationTask
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
