using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;

// XXX : some properties are used only on the UI, should separate them but since it a Tree structure, it will be hard
namespace Automation.Base
{
    public enum EnumNodeType
    {
        Scope,
        Workflow,
        Task
    }

    public class Node : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? ParentId { get; set; }

        public string Name { get; set; }
        public EnumNodeType Type { get; set; }
    }

    public class Scope : Node
    {
        public ObservableCollection<Node> Childrens { get; set; } = [];

        public Scope() { Type = EnumNodeType.Scope; }

        public void AddChild(Node child)
        {
            child.Parent = this;
            Childrens.Add(child);
        }
    }

    public class TaskNode : Node
    {
        public Point Location { get; set; }

        public ObservableCollection<NodeConnector> Inputs { get; set; } = new ObservableCollection<NodeConnector>();
        public ObservableCollection<NodeConnector> Outputs { get; set; } = new ObservableCollection<NodeConnector>();

        public TaskNode()
        {
            Type = EnumNodeType.Task;
        }
    }

    public class WorkflowNode : TaskNode
    {
        public ObservableCollection<NodeConnection> Connections { get; } = new ObservableCollection<NodeConnection>();
        public List<Guid> Nodes { get; set; }

        public WorkflowNode()
        {
            Type = EnumNodeType.Workflow;
        }
    }

    public class NodeConnector
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
    }

    public class NodeConnection
    {
        public Guid SourceConnectorId { get; set; }
        public Guid TargetConnectorId { get; set; }
    }
}
