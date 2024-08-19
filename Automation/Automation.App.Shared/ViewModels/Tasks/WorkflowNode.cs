using Automation.Shared.Data;
using System.Collections.ObjectModel;
using System.Windows;

namespace Automation.App.Shared.ViewModels.Tasks
{
    public class WorkflowNode : TaskNode, IWorkflowNode
    {
        public ObservableCollection<TaskConnection> TaskConnections
        {
            get;
            set;
        } = new ObservableCollection<TaskConnection>();

        public IEnumerable<ITaskConnection> Connections => TaskConnections;

        public IEnumerable<ILinkedNode> Nodes { get; private set; } = new ObservableCollection<ILinkedNode>();
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

    public class TaskConnection : ITaskConnection
    {
        public Guid ParentId { get; set; }

        public Guid SourceId { get; set; }

        public Guid TargetId { get; set; }

        public TaskConnector Source { get; set; }
        public TaskConnector Target { get; set; }
    }
}
