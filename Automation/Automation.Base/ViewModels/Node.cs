using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;

namespace Automation.Base.ViewModels
{
    public interface INode
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Point Location { get; set; }
    }

    public class NodeGroup : INode
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public Point Location { get; set; }
        public Size Size { get; set; }
    }

    [Flags]
    public enum EnumConnectorsDirection
    {
        None = 0,
        In = 1,
        Out = 2,
    }

    public enum EnumNodeConnectorType
    {
        Data,
        Flow
    }

    public class TaskNode : ScopedElement, INode
    {
        public Point Location { get; set; }
        [JsonIgnore]
        public List<NodeConnector> Inputs { get; set; } = [];
        [JsonIgnore]
        public List<NodeConnector> Outputs { get; set; } = [];

        public EnumConnectorsDirection AllowedConnectorEdits { get; set; } = EnumConnectorsDirection.None;

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

    public partial class NodeConnector : INotifyPropertyChanged
    {
        public EnumNodeConnectorType Type { get; set; } = EnumNodeConnectorType.Data;

        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public Guid ParentId { get; set; }

        [JsonIgnore]
        public bool IsConnected { get; set; }
        [JsonIgnore]
        public Point Anchor { get; set; }
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
            return connector is NodeInput && connector.Parent.AllowedConnectorEdits.HasFlag(EnumConnectorsDirection.In) ||
                   connector is NodeOutput && connector.Parent.AllowedConnectorEdits.HasFlag(EnumConnectorsDirection.Out);
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
