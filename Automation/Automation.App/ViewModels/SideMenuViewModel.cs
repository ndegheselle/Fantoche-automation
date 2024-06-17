using Automation.App.ViewModels.Graph;
using Automation.App.ViewModels.Scopes;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Automation.App.ViewModels
{
    public class SideMenuViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName]string? propertyName = null)
        { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); }

        public Scope RootScope { get; set; }

        private ScopedElement? _selectedElement;

        public ScopedElement? SelectedElement
        {
            get => _selectedElement;
            set
            {
                if (_selectedElement == value)
                    return;

                _selectedElement = value;
                OnPropertyChanged();
            }
        }

        public SideMenuViewModel()
        {
            TaskScope taskScope1 = new TaskScope()
            {
                Name = "Task 1",
                Inputs = new ObservableCollection<ElementConnector>() { new ElementConnector() { Name = "Input 1" }, },
                Outputs =
                    new ObservableCollection<ElementConnector>() { new ElementConnector() { Name = "Output 1" }, },
            };

            TaskScope taskScope2 = new TaskScope()
            {
                Name = "Task 2",
                Inputs = new ObservableCollection<ElementConnector>() { new ElementConnector() { Name = "Input 1" }, },
                Outputs =
                    new ObservableCollection<ElementConnector>() { new ElementConnector() { Name = "Output 1" }, },
            };

            WorkflowScope workflowScope = new WorkflowScope() { Name = "Workflow 1", };
            workflowScope.Nodes.Add(taskScope1);
            workflowScope.Nodes.Add(taskScope2);

            workflowScope.Connections
                .Add(new ElementConnection(taskScope2.Outputs[0], taskScope1.Inputs[0]));

            Scope subScope = new Scope() { Name = "SubScope 1", };
            subScope.AddChild(taskScope1);

            Scope mainScope = new Scope()
            {
                Name = "Scope 1",
            };
            mainScope.AddChild(subScope);
            mainScope.AddChild(workflowScope);
            mainScope.AddChild(taskScope2);

            RootScope = new Scope();
            RootScope.AddChild(mainScope);
        }
    }
}
