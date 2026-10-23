using Fantoche.App.Common;
using Fantoche.App.Features.Packages.Controls;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Scoped;
using Fantoche.Shared.Services;
using Fantoche.App.Features.Workflows.Details.Controls;
using CommunityToolkit.Mvvm.Input;
using Joufflu.Feedback;
using Joufflu.Navigation;

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

        /// <summary>See <see cref="TaskSettings.StorageRetention"/>.</summary>
        public int StorageRetention
        {
            get => Task.Settings.StorageRetention;
            set
            {
                Task.Settings.StorageRetention = Math.Max(value, 0);
                OnPropertyChanged();
                MarkChanged();
            }
        }

        public TaskDetailsViewModel(
            ScopedNode node,
            WorkflowsViewModel parent,
            IScopedService scoped,
            IHistoryService history,
            IOverlayer overlays,
            IToastService toasts)
            : base(node, parent, scoped, history, overlays, toasts)
        {
            Schedules = new SchedulesViewModel(Task, overlays, () => SaveElementAsync($"The schedules of the task '{Node.Name}' have been saved."));
        }

        /// <summary>When the task is started on its own, displayed by the schedules tab.</summary>
        public SchedulesViewModel Schedules { get; }

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
