using System.ComponentModel;
using System.Drawing;
using System.Text.Json.Serialization;
using System.Windows;

namespace Automation.Shared.ViewModels
{
    public interface INode
    {
        public Guid Id { get; set; }
        public string Name { get; }
    }

    public class NodeGroup : INode
    {
        public Size Size { get; set; }
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Point Location { get; set; }
    }

    public enum EnumTaskConnectorType
    {
        Data,
        Flow
    }

    public enum EnumTaskConnectorDirection
    {
        In,
        Out
    }

    public class TaskNode : INode
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        // Parent scope name ?
        public string Name { get; set; }

        [JsonIgnore]
        public ScopedTask ParentScope { get; set; }

        [JsonIgnore]
        public List<TaskConnector> Inputs { get; set; } = [];
        [JsonIgnore]
        public List<TaskConnector> Outputs { get; set; } = [];

        public void AddInput(TaskConnector input)
        {
            input.Parent = this;
            input.Direction = EnumTaskConnectorDirection.In;
            Inputs.Add(input);
        }

        public void AddOutput(TaskConnector output)
        {
            output.Parent = this;
            output.Direction = EnumTaskConnectorDirection.Out;
            Outputs.Add(output);
        }
    }

    public class TaskConnector : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public EnumTaskConnectorType Type { get; set; } = EnumTaskConnectorType.Data;
        public EnumTaskConnectorDirection Direction { get; set; } = EnumTaskConnectorDirection.In;

        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public Guid ParentId { get; set; }
        [JsonIgnore]
        public TaskNode Parent { get; set; }
    }
}
