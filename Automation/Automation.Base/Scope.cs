using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;

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
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? ParentId { get; set; }
        public Scope? Parent { get; set; }

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
        public ObservableCollection<TaskNode> Nodes { get; } = new ObservableCollection<TaskNode>();
        public ObservableCollection<NodeConnection> Connections { get; } = new ObservableCollection<NodeConnection>();

        public WorkflowNode()
        {
            Type = EnumNodeType.Workflow;
        }
    }

    public class NodeConnector : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
    }

    public class NodeConnection : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public Guid IdSource { get; set; }
        public NodeConnector Source { get; set; }

        public Guid IdTarget { get; set; }
        public NodeConnector Target { get; set; }

        public NodeConnection(NodeConnector source, NodeConnector target)
        {
            Source = source;
            Target = target;
        }
    }
}
