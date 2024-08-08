using Automation.Shared.Contracts;
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
        public IList<ITaskConnector> Connectors { get; set; } = new List<ITaskConnector>();
    }
}
