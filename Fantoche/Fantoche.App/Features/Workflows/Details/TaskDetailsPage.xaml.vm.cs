using Fantoche.App.Common;
using Fantoche.App.Features.Packages.Controls;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Scoped;
using CommunityToolkit.Mvvm.Input;

namespace Fantoche.App.Features.Workflows.Details
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

        public TaskDetailsViewModel(ScopedNode node, WorkflowsViewModel parent, AppServices services)
            : base(node, parent, services)
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
