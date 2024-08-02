using System.ComponentModel;
using System.Drawing;
using System.Text.Json.Serialization;

namespace Automation.Shared.Data
{
    #region Interfaces
    public interface INodeGroup
    {
        Size Size { get; set; }
        Guid Id { get; set; }
        string Name { get; set; }
        Point Location { get; set; }
    }

    public interface ITaskNode
    {
        Guid Id { get; set; }
        Guid ScopeId { get; set; }
        string Name { get; set; }
        List<ITaskConnector> Connectors { get; }
    }

    public interface ITaskConnector
    {
        EnumTaskConnectorType Type { get; set; }
        EnumTaskConnectorDirection Direction { get; set; }
        Guid Id { get; set; }
        string Name { get; set; }
        Guid ParentId { get; set; }
    }
    #endregion

    public class NodeGroup 
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
