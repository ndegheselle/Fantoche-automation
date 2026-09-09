using Automation.App.Features.Packages.Controls;
using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Scoped;
using CommunityToolkit.Mvvm.Input;

namespace Automation.App.Features.Workflows.Details
{
    public partial class TaskDetailsViewModel : ScopedDetailsViewModel<AutomationTask>
    {
        public AutomationTask Task => Element;
        public PackageClassTarget? Target
        {
            get => Task.Target;
            private set
            {
                Task.Target = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasTarget));
                MarkChanged();
            }
        }

        public bool HasTarget => Target != null;

        public TaskDetailsViewModel(ScopedNode node, WorkflowsViewModel parent) : base(node, parent)
        { }

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
