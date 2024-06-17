using Automation.App.ViewModels.Graph;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace Automation.App.ViewModels.Scopes
{

    public class TaskScope : ScopedElement
    {
        public Point Location { get; set; }

        public ObservableCollection<ElementConnector> Inputs { get; set; } = new ObservableCollection<ElementConnector>();
        public ObservableCollection<ElementConnector> Outputs { get; set; } = new ObservableCollection<ElementConnector>();

        public TaskScope()
        {
            Type = EnumTaskType.Task;
        }
    }

    public class WorkflowScope : TaskScope
    {
        public ObservableCollection<TaskScope> Nodes { get; } = new ObservableCollection<TaskScope>();
        public ObservableCollection<ElementConnection> Connections { get; } = new ObservableCollection<ElementConnection>();

        public WorkflowScope()
        {
            Type = EnumTaskType.Workflow;
        }
    }
}
