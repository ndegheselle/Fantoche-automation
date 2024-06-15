using Automation.App.ViewModels.Graph;
using System.Collections.ObjectModel;

namespace Automation.App.ViewModels.Scopes
{

    public class TaskScope : ScopedElement
    {
        public ObservableCollection<ElementEndpoint> Inputs { get; set; } = new ObservableCollection<ElementEndpoint>();
        public ObservableCollection<ElementEndpoint> Outputs { get; set; } = new ObservableCollection<ElementEndpoint>();

        public TaskScope()
        {
            Type = EnumTaskType.Task;
        }
    }

    public class WorkflowScope : TaskScope
    {
        public ObservableCollection<TaskScope> Nodes { get; } = new ObservableCollection<TaskScope>();
        public ObservableCollection<ElementLink> Links { get; } = new ObservableCollection<ElementLink>();

        public WorkflowScope()
        {
            Type = EnumTaskType.Workflow;
        }
    }
}
