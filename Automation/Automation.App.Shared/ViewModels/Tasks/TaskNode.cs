using Automation.Shared.Data;
using System.Windows;

namespace Automation.App.Shared.ViewModels.Tasks
{
    public class TaskNode : ITaskNode
    {
        public Guid Id { get; set; }
        public Guid ScopeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public IEnumerable<ITaskConnector> Connectors { get; private set; } = new List<TaskConnector>();
    }

    public class TaskConnector : ITaskConnector
    {
        public EnumTaskConnectorType Type { get; set; }

        public EnumTaskConnectorDirection Direction { get; set; }

        public Guid Id { get; set; }

        public string Name { get; set; }

        public Guid ParentId { get; set; }

        // Ui specifics

        public ITaskNode Parent { get; set; }
        public bool IsConnected { get; set; }
        public Point Anchor { get; set; }
    }
}
