using Automation.Shared.Data.Scoped;
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

    public class TaskDetailsViewModel
    {
        public ScopedNode Node { get; }
        public AutomationTask Task => (AutomationTask)Node.Element;

        public TaskDetailsViewModel(ScopedNode node)
        {
            Node = node;
        }
    }
}
