using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Data;
using System.Windows.Markup;

namespace Automation.Base
{
    public enum EnumNodeType
    {
        Scope,
        Workflow,
        Task
    }

    public partial class Node
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Scope? Parent { get; set; }

        public string Name { get; set; }
        public EnumNodeType Type { get; set; }
    }

    public partial class Scope : Node
    {
        public ObservableCollection<Node> Childrens { get; set; } = [];
        public Scope() { 
            Type = EnumNodeType.Scope;
            // UI specific
            SortedChildrens = (ListCollectionView)CollectionViewSource.GetDefaultView(Childrens);
            SortedChildrens.SortDescriptions.Add(new SortDescription(nameof(Type), ListSortDirection.Ascending));
            SortedChildrens.SortDescriptions.Add(new SortDescription(nameof(Name), ListSortDirection.Ascending));
        }

        public void AddChild(Node node)
        {
            node.Parent = this;
            Childrens.Add(node);
        }
    }

    public partial class TaskNode : Node
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
        public ObservableCollection<NodeConnection> Connections { get; } = [];
        public ObservableCollection<Node> Nodes { get; set; } = [];

        public WorkflowNode()
        {
            Type = EnumNodeType.Workflow;
        }
    }

    public partial class NodeConnector
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
    }

    public partial class NodeConnection
    {
        public NodeConnector Source { get; set; }
        public NodeConnector Target { get; set; }
    }
}
