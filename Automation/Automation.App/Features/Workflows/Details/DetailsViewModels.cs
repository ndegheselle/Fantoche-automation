using Automation.App.Features.Packages.Controls;
using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Scoped;
using Automation.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Joufflu.Feedback.Controls;
using Joufflu.Navigation;

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
                OnPropertyChanged(nameof(TargetPackage));
            }
        }

        public bool HasTarget => Target != null;

        /// <summary>
        /// Target as package informations, to be displayed by a
        /// <see cref="Packages.Controls.PackageSummary"/>. The targeted class takes the description
        /// slot, the package description not being stored on the task.
        /// </summary>
        public PackageInfos? TargetPackage => Target == null
            ? null
            : new PackageInfos() { Identifier = Target.Package, Description = Target.ClassFullName };

        private readonly WorkflowsViewModel _parent;
        private readonly IScopedService _scoped = SpineViewModel.Instance.Scoped;
        private readonly IOverlayService _overlays = SpineViewModel.Instance.Overlays;
        private readonly IToastService _toasts = SpineViewModel.Instance.Toasts;

        public TaskDetailsViewModel(ScopedNode node, WorkflowsViewModel parent)
        {
            Node = node;
            _parent = parent;
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

        [RelayCommand]
        public async Task Save()
        {
            await _scoped.EditAsync(Task);
            _toasts.Success($"The task '{Node.Name}' has been saved.", "Task saved");
        }

        [RelayCommand]
        public async Task Delete()
        {
            if (await _overlays.Confirm($"Are you sure you want to delete the task '{Node.Name}' ?", "Confirm deletion", EnumConfirmationType.Danger) != true)
                return;

            await _scoped.RemoveAsync(Task);
            Node.Parent?.Children.Remove(Node);
            // Fall back on the parent scope, the task not being displayable anymore
            _parent.Open(Node.Parent);
        }
    }
}
