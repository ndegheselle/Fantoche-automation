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
                OnPropertyChanged(nameof(TargetPackage));
                MarkChanged();
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
