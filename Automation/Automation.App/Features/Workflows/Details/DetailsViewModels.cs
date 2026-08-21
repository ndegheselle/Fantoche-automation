using Automation.App.Features.Packages.Controls;
using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Scoped;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Automation.App.Features.Workflows.Details
{
    public class ScopeDetailsViewModel
    {
        public ScopedNode Node { get; }
        public Scope Scope => (Scope)Node.Element;
        public IRelayCommand<ScopedNode?> OpenCommand { get; }

        public ScopeDetailsViewModel(ScopedNode node, WorkflowsViewModel parent)
        {
            Node = node;
            OpenCommand = parent.OpenCommand;
        }
    }

    public class WorkflowDetailsViewModel
    {
        public ScopedNode Node { get; }
        public AutomationWorkflow Workflow => (AutomationWorkflow)Node.Element;
        public IRelayCommand<ScopedNode?> OpenCommand { get; }

        public WorkflowDetailsViewModel(ScopedNode node, WorkflowsViewModel parent)
        {
            Node = node;
            OpenCommand = parent.OpenCommand;
        }
    }

    public partial class TaskDetailsViewModel : ObservableObject
    {
        public ScopedNode Node { get; }
        public AutomationTask Task => (AutomationTask)Node.Element;

        public PackageClassTarget? Target
        {
            get => Task.Target;
            private set
            {
                Task.Target = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasTarget));
            }
        }

        public bool HasTarget => Target != null;

        public TaskDetailsViewModel(ScopedNode node)
        {
            Node = node;
        }

        [RelayCommand]
        public async Task SelectPackage()
        {
            var target = await PackageSelectionViewModel.ShowAsync();
            if (target == null)
                return;
            Target = target;
        }

        [RelayCommand]
        public void RemovePackage() => Target = null;
    }
}
