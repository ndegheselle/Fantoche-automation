using Automation.Shared.Data;
using MongoDB.Bson.Serialization.Attributes;

namespace Automation.Dal.Models
{
    public class TaskNode : ITaskNode
    {
        [BsonId]
        public Guid Id { get; set; }
        [BsonId]
        public Guid ScopeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public IEnumerable<ITaskConnector> Connectors { get; set; } = new List<TaskConnectors>();
    }

    public class TaskConnectors : ITaskConnector
    {
        public Guid Id { get; set; }
        public string Name {get;set;}
        public EnumTaskConnectorType Type {get;set;}
        public EnumTaskConnectorDirection Direction {get;set;}
    }
}
