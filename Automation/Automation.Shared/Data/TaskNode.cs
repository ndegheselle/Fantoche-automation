using System.ComponentModel;
using System.Drawing;
using System.Text.Json.Serialization;

namespace Automation.Shared.Data
{
    public class NodeGroup
    {
        public Size Size { get; set; }
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Point Location { get; set; }
    }

    [JsonDerivedType(typeof(WorkflowNode), typeDiscriminator: "workflow")]
    public class TaskNode
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ScopeId { get; set; }

        // Parent scope name ?
        public string Name { get; set; }
        public List<TaskConnector> Connectors { get; set; } = [];
    }

    public class TaskConnector : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public EnumTaskConnectorType Type { get; set; } = EnumTaskConnectorType.Data;
        public EnumTaskConnectorDirection Direction { get; set; } = EnumTaskConnectorDirection.In;

        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public Guid ParentId { get; set; }
    }
}
