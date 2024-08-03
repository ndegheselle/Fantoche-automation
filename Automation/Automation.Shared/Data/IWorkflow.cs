using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automation.Shared.Data
{
    public interface IWorkflowNode : ITaskNode
    {
        IList<ITaskConnection> Connections { get; }
        IList<ILinkedNode> Nodes { get; }
    }

    // Represent a node that is linked to a workflow
    public interface ILinkedNode : INode
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
