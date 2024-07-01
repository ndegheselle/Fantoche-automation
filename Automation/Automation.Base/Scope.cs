using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Xml.Linq;

namespace Automation.Base
{
    [Flags]
    public enum EnumNodeType
    {
        Scope = 1,
        Workflow = 2,
        Task = 4,
        Group = 8,
        FlowTask = 16,
    }

    [JsonDerivedType(typeof(Scope), typeDiscriminator: "scope")]
    [JsonDerivedType(typeof(TaskNode), typeDiscriminator: "task")]
    [JsonDerivedType(typeof(WorkflowNode), typeDiscriminator: "workflow")]
    [JsonDerivedType(typeof(NodeGroup), typeDiscriminator: "group")]
    public partial class Node
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid? ParentId { get; set; }
        [JsonIgnore]
        public Scope? Parent { get; set; }

        public string Name { get; set; }
        public EnumNodeType Type { get; set; }
    }

    public class NodeGroup : Node
    {
        public Point Location { get; set; }
        public Size Size { get; set; }
        public NodeGroup()
        {
            Type = EnumNodeType.Group;
        }
    }

    // TODO : separate Scope from node
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
        public Point Location { get; set; }
        [JsonIgnore]
        public List<NodeConnector> Inputs { get; set; } = [];
        [JsonIgnore]
        public List<NodeConnector> Outputs { get; set; } = [];

        public EnumTaskNodeConnectorsOptions ConnectorsOptions { get; set; } = EnumTaskNodeConnectorsOptions.None;

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

    public class WorkflowInputNode : TaskNode
    {
        public WorkflowInputNode() : base()
        {
            Name = "Start";
            Type = EnumNodeType.FlowTask;
            ConnectorsOptions = EnumTaskNodeConnectorsOptions.EditInputs;
        }
    }
    public class WorkflowOutputNode : TaskNode
    {
        public WorkflowOutputNode() : base()
        {
            Name = "End";
            Type = EnumNodeType.FlowTask;
            ConnectorsOptions = EnumTaskNodeConnectorsOptions.EditOutputs;
        }
    }

    public class WorkflowNode : TaskNode
    {
        public ObservableCollection<NodeConnection> Connections { get; } = [];
        [JsonIgnore]
        public ObservableCollection<Node> Nodes { get; set; } = [];

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
        public Guid ParentId { get; set; }

        [JsonIgnore]
        public TaskNode Parent { get; set; }

        public ICommand Delete { get; set; } = new DelegateCommand<NodeConnector>(connector =>
        {
            if (connector is NodeInput input)
            {
                connector.Parent.Inputs.Remove(input);
            }
            else if (connector is NodeOutput output)
            {
                connector.Parent.Outputs.Remove(output);
            }
        }, (connector) =>
        {
            return connector.Parent.ConnectorsOptions.HasFlag(EnumTaskNodeConnectorsOptions.EditInputs) ||
                   connector.Parent.ConnectorsOptions.HasFlag(EnumTaskNodeConnectorsOptions.EditOutputs);
        });
    }

    public class NodeInput : NodeConnector
    { }
    public class NodeOutput : NodeConnector
    { }

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
