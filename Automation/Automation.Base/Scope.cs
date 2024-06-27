using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Text.Json.Serialization;
using System.Windows.Data;
using System.Windows.Markup;

namespace Automation.Base
{
    [Flags]
    public enum EnumNodeType
    {
        Scope = 1,
        Workflow = 2,
        Task = 4
    }

    [JsonDerivedType(typeof(Scope), typeDiscriminator: "scope")]
    [JsonDerivedType(typeof(TaskNode), typeDiscriminator: "task")]
    [JsonDerivedType(typeof(WorkflowNode), typeDiscriminator: "workflow")]
    public partial class Node
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid? ParentId { get; set; }
        [JsonIgnore]
        public Scope? Parent { get; set; }

        public string Name { get; set; }
        public EnumNodeType Type { get; set; }
    }

    public partial class Scope : Node
    {
        [JsonIgnore]
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
        [JsonIgnore]
        public List<NodeConnector> Inputs { get; set; } = [];
        [JsonIgnore]
        public List<NodeConnector> Outputs { get; set; } = [];

        public TaskNode()
        {
            Type = EnumNodeType.Task;
        }

        public void AddInput(NodeConnector input)
        {
            input.Parent = this;
            Inputs.Add(input);
        }

        public void AddOutput(NodeConnector output)
        {
            output.Parent = this;
            Outputs.Add(output);
        }
    }

    public class WorkflowNode : TaskNode
    {
        [JsonIgnore]
        public ObservableCollection<NodeConnection> Connections { get; } = [];
        [JsonIgnore]
        public ObservableCollection<TaskNode> Tasks { get; set; } = [];

        public WorkflowNode()
        {
            Type = EnumNodeType.Workflow;
        }

        public void AddConnection(NodeConnection connection)
        {
            connection.ParentWorkflow = this;
            Connections.Add(connection);
        }
    }

    public class WorkflowRelation
    {
        public Guid WorkflowId { get; set; }
        public Guid NodeId { get; set; }
    }

    [JsonDerivedType(typeof(NodeInput), typeDiscriminator: "input")]
    [JsonDerivedType(typeof(NodeOutput), typeDiscriminator: "output")]
    public partial class NodeConnector
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public Type Type { get; set; }

        public Guid ParentId { get; set; }
        [JsonIgnore]
        public Node Parent { get; set; }
    }

    public class NodeInput : NodeConnector
    {}
    public class NodeOutput : NodeConnector
    {}

    public partial class NodeConnection
    {
        public Guid ParentId { get; set; }
        public Guid SourceId { get; set; }
        public Guid TargetId { get; set; }


        [JsonIgnore]
        public WorkflowNode ParentWorkflow { get; set; }
        [JsonIgnore]
        public NodeConnector Source { get; set; }
        [JsonIgnore]
        public NodeConnector Target { get; set; }

        // Deserialization
        public NodeConnection() { }

        public NodeConnection(WorkflowNode parent, NodeConnector source, NodeConnector target)
        {
            ParentId = parent.Id;
            SourceId = source.Id;
            TargetId = target.Id;

            ParentWorkflow = parent;
            Source = source;
            Target = target;

            source.IsConnected = true;
            target.IsConnected = true;
        }
    }
}
