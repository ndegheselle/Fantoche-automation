using Automation.Shared.Data;
using System.Drawing;

namespace Automation.Dal.Models
{
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

    public class Graph : IGraph, IIdentifier
    {
        public Guid Id { get; set; }
        public Guid WorkflowId { get; set; }
        public IEnumerable<ITaskConnection> Connections { get; set; } = new List<TaskConnection>();
        public IEnumerable<ILinkedNode> Nodes { get; set; } = new List<ILinkedNode>();
        public IEnumerable<INodeGroup> Groups { get; set; } = new List<INodeGroup>();
    }
}
