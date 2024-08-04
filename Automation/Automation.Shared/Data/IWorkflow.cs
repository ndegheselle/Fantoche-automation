using System.Drawing;

namespace Automation.Shared.Data
{
    public interface IWorkflowNode : ITaskNode
    {
        IList<ITaskConnection> Connections { get; }
        IList<ILinkedNode> Nodes { get; }
    }

    public interface INodeGroup : ILinkedNode
    {
        Size Size { get; set; }
        Point Location { get; set; }
    }

    // Represent a node that is linked to a workflow
    public interface ILinkedNode : INamed
    {
        Point Position { get; set; }
    }

    public interface IRelatedTaskNode : ITaskNode, ILinkedNode { }
    public interface IRelatedWorkflowNode : IWorkflowNode, ILinkedNode { }

    public interface ITaskConnection
    {
        Guid ParentId { get; set; }
        Guid SourceId { get; set; }
        Guid TargetId { get; set; }
    }
}
