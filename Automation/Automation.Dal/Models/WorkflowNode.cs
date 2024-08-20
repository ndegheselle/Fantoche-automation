using Automation.Shared.Data;
using MongoDB.Bson.Serialization.Attributes;
using System.Drawing;

namespace Automation.Dal.Models
{
    public class WorkflowNode : TaskNode, IWorkflowNode
    {
        public List<NodeGroup> Groups { get; set;} = new List<NodeGroup>();
        public Dictionary<Guid, WorkflowRelation> TaskNodeChildrens { get; set; } = new Dictionary<Guid, WorkflowRelation>();

        public IEnumerable<ITaskConnection> Connections { get; set; } = new List<TaskConnection>();

        [BsonIgnore]
        public IEnumerable<ILinkedNode> Nodes { get; set; } = new List<ILinkedNode>();
    }

    public class RelatedTaskNode : ILinkedNode
    {
        public Guid Id => Node.Id;
        public string Name => Node.Name;
        public WorkflowRelation WorkflowContext {get;set;}
        public TaskNode Node { get; set; }
    }

    public class NodeGroup : INodeGroup
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Size Size { get; set; }
        public WorkflowRelation WorkflowContext { get; set; }
    }

    public class TaskConnection : ITaskConnection
    {
        public Guid ParentId {get;set;}
        public Guid SourceId {get;set;}
        public Guid TargetId {get;set;}
    }
}
