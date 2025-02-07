using System.Drawing;

namespace Automation.Shared.Data
{
    public interface IGraphNode : INamed
    {
        Point Position { get; set; }
    }

    public interface IGraphGroup : IGraphNode
    {
        Size Size { get; set; }
    }

    public interface IGraphConnection
    {
        Guid SourceId { get; set; }
        Guid TargetId { get; set; }
    }

    public interface IGraph
    {
        IEnumerable<IGraphConnection> Connections { get; }
        IEnumerable<IGraphNode> Nodes { get; }
        IEnumerable<IGraphGroup> Groups { get; }
    }
}
