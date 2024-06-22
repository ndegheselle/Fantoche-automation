using System.Drawing;

namespace Automation.Base
{
    public enum EnumNodeType
    {
        Scope,
        Workflow,
        Task
    }

    public class Node
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Scope? Parent { get; set; }

        public string Name { get; set; }
        public EnumNodeType Type { get; set; }
    }

    public class Scope : Node
    {
        public List<Node> Childrens { get; set; } = [];
        public Scope() { Type = EnumNodeType.Scope; }

        public void AddChild(Node node)
        {
            node.Parent = this;
            Childrens.Add(node);
        }
    }

    public class TaskNode : Node
    {
        public List<NodeConnector> Inputs { get; set; } = [];
        public List<NodeConnector> Outputs { get; set; } = [];

        public TaskNode()
        {
            Type = EnumNodeType.Task;
        }
    }

    public class WorkflowNode : TaskNode
    {
        public List<NodeConnection> Connections { get; } = [];
        public List<Node> Nodes { get; set; } = [];

        public WorkflowNode()
        {
            Type = EnumNodeType.Workflow;
        }
    }

    public class NodeConnector
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }

        // TODO : move to a UI specific class ? Means a TaskWrapper, NodeConnectionWrapper, NodeConnectorWrapperK.
        #region UI Specific
        public bool IsConnected { get; set; }
        public Point Anchor { get; set; }
        #endregion
    }

    public class NodeConnection
    {
        public NodeConnector Source { get; set; }
        public NodeConnector Target { get; set; }

        public NodeConnection(NodeConnector source, NodeConnector target)
        {
            Source = source;
            Target = target;
        }
    }
}
