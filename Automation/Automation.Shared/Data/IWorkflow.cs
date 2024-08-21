using System.Drawing;

namespace Automation.Shared.Data
{
    public interface IWorkflowNode : ITaskNode
    {
        IEnumerable<ITaskConnection> Connections { get; }
        IEnumerable<ILinkedNode> Nodes { get; }
    }

    public interface ILinkedNode : INamed
    {
        Point Position { get; set; }
    }

    // Represent a node that is linked to a workflow
    public interface IRelatedTaskNode : ILinkedNode
    {
        ITaskNode Node { get; }
    }

    public interface INodeGroup : ILinkedNode
    {
        Size Size { get; set; }
    }

    public interface ITaskConnection
    {
        Guid ParentId { get; set; }
        Guid SourceId { get; set; }
        Guid TargetId { get; set; }
    }
}
