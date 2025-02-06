using System.Drawing;

namespace Automation.Shared.Data
{
    public interface ILinkedNode : INamed
    {
        Point Position { get; set; }
    }

    public interface INodeGroup : ILinkedNode
    {
        Size Size { get; set; }
    }

    public interface ITaskConnection
    {
        Guid SourceId { get; set; }
        Guid TargetId { get; set; }
    }

    public interface IGraph
    {
        IEnumerable<ITaskConnection> Connections { get; }
        IEnumerable<ILinkedNode> Nodes { get; }
        IEnumerable<INodeGroup> Groups { get; }
    }
}
