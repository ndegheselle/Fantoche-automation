using Automation.Shared.Data;
using System.Drawing;

namespace Automation.Dal.Models
{
    public class NodeGroup : IGraphGroup
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Size Size { get; set; }
        public Point Position { get; set; }
    }

    public class TaskConnection : IGraphConnection
    {
        public Guid ParentId {get;set;}
        public Guid SourceId {get;set;}
        public Guid TargetId {get;set;}
    }

    public class Graph : IGraph, IIdentifier
    {
        public Guid Id { get; set; }
        public Guid WorkflowId { get; set; }
        public IEnumerable<IGraphConnection> Connections { get; set; } = new List<TaskConnection>();
        public IEnumerable<IGraphNode> Nodes { get; set; } = new List<IGraphNode>();
        public IEnumerable<IGraphGroup> Groups { get; set; } = new List<IGraphGroup>();
    }
}
