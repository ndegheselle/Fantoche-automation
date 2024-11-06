using Automation.Shared.Data;
using System.ComponentModel;
using System.Windows;

namespace Automation.App.Shared.ViewModels.Tasks
{
    public class TaskNode : ScopedElement, INotifyPropertyChanged
    {
        public TargetedPackage? Package { get; set; }
        public List<TaskConnector> Inputs { get; set; } = [];
        public List<TaskConnector> Outputs { get; set; } = [];

        public TaskNode()
        {
            Type = EnumScopedType.Task;
        }
    }

    public class TaskConnector : ITaskConnector
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid ParentId { get; set; }
        public EnumTaskConnectorType Type { get; set; }
        public EnumTaskConnectorDirection Direction { get; set; }

        // Ui specifics
        public TaskNode Parent { get; set; }
        public bool IsConnected { get; set; }
        public Point Anchor { get; set; }
    }
}
