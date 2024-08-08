using Automation.Shared.Contracts;
using MongoDB.Bson.Serialization.Attributes;

namespace Automation.Dal.Models
{
    public class WorkflowNode : IWorkflowNode
    {
        [BsonId]
        public Guid Id { get; set; }
        [BsonId]
        public Guid ScopeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public IList<ITaskConnector> Connectors { get; private set; } = new List<ITaskConnector>();
        public IList<ITaskConnection> Connections { get; private set; } = new List<ITaskConnection>();
        public IList<ILinkedNode> Nodes { get; private set; } = new List<ILinkedNode>();
    }
}
