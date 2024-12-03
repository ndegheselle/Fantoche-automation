using Automation.Shared.Data;
using System.ComponentModel;
using System.Windows;

namespace Automation.App.Shared.ViewModels.Tasks
{
    public class TaskNode : ScopedElement, ITaskNode, INotifyPropertyChanged
    {
        public TargetedPackage? Package { get; set; }
        public List<TaskConnector> Inputs { get; set; } = [];
        public List<TaskConnector> Outputs { get; set; } = [];
        public List<Schedule> Schedules { get; set; } = [];

        IEnumerable<ITaskConnector> ITaskNode.Inputs => Inputs;
        IEnumerable<ITaskConnector> ITaskNode.Outputs => Outputs;
        IEnumerable<Schedule> ITaskNode.Schedules => Schedules;

        public TaskNode()
        {
            Type = EnumScopedType.Task;
        }
    }

    public class TaskConnector : ITaskConnector
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid ParentId { get; set; }
        public EnumTaskConnectorType Type { get; set; }
        public EnumTaskConnectorDirection Direction { get; set; }

        // Ui specifics
        public TaskNode Parent { get; set; } = new TaskNode();
        public bool IsConnected { get; set; }
        public Point Anchor { get; set; }
    }
}
