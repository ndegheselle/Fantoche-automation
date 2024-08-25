using Automation.Shared.Data;
using MongoDB.Bson.Serialization.Attributes;
using System.Drawing;

namespace Automation.Dal.Models
{
    public class WorkflowNode : TaskNode, IWorkflowNode
    {
        public IEnumerable<ITaskConnection> Connections { get; set; } = new List<TaskConnection>();
        public IEnumerable<ILinkedNode> Nodes { get; set; } = new List<ILinkedNode>();
    }

    public class RelatedTaskNode : IRelatedTaskNode
    {
        public Guid Id { get; set; }
        public Point Position {  get; set; }
        [BsonIgnore]
        public string Name => Node.Name;
        [BsonIgnore]
        public ITaskNode Node { get; set; }
    }

    public class NodeGroup : INodeGroup
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Size Size { get; set; }
        public Point Position { get; set; }
    }

    public class TaskConnection : ITaskConnection
    {
        public Guid ParentId {get;set;}
        public Guid SourceId {get;set;}
        public Guid TargetId {get;set;}
    }
}
