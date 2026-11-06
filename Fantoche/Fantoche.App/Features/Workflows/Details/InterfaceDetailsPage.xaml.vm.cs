using Fantoche.App.Features.Packages.Controls;
using Fantoche.App.Features.Workflows.Controls;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Scoped;
using Fantoche.Shared.Services;
using CommunityToolkit.Mvvm.Input;
using Joufflu.Feedback;
using Joufflu.Navigation;

namespace Fantoche.App.Features.Workflows.Details
{
    /// <summary>
    /// An interface of a task (see <see cref="AutomationInterface"/>) : the class it points at, and
    /// the task it is linked to by hanging under it.
    /// </summary>
    public partial class InterfaceDetailsViewModel : ScopedDetailsViewModel<AutomationInterface>
    {
        private readonly IToastService _toasts;

        public AutomationInterface Interface => Element;

        /// <summary>The built-in form generated from the parameters, which points at no class and links to no task.</summary>
        public bool IsForm => Interface.IsForm;

        public PackageClassTarget? Target
        {
            get => Interface.Target;
            private set
            {
                Interface.Target = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasTarget));
                MarkChanged();
            }
        }

        public bool HasTarget => Target != null;

        /// <summary>The task it hangs under, which shows and asks with it ; null while it is linked to none.</summary>
        public AutomationTask? LinkedTask => Node.Parent?.Element as AutomationTask;

        public bool IsLinked => LinkedTask != null;

        public InterfaceDetailsViewModel(
            ScopedNode node,
            WorkflowsViewModel parent,
            IScopedService scoped,
            IHistoryService history,
            IOverlayer overlays,
            IToastService toasts)
            : base(node, parent, scoped, history, overlays, toasts)
        {
            _toasts = toasts;
        }

        [RelayCommand]
        private async Task SelectClass()
        {
            PackageClassTarget? target = await PackageSelectionViewModel.ShowInterfacesAsync();
            if (target == null)
                return;
            Target = target;
        }

        [RelayCommand]
        private void RemoveClass() => Target = null;

        /// <summary>Hang the interface under a task : the task then shows and asks with it.</summary>
        [RelayCommand]
        private async Task Link()
        {
            BaseAutomationTask? picked = await TaskSelectionViewModel.ShowAsync();
            if (picked == null)
                return;
            if (picked is not AutomationTask task || picked is AutomationControl)
            {
                _toasts.Warning("Only a task shows the parameters it asks for : a workflow or a control can't be given an interface.", "Pick a task");
                return;
            }

            await MoveAsync(task.Id, $"The interface '{Node.Name}' is linked to the task '{task.Metadata.Name}'.");
        }

        /// <summary>Take the interface back from its task, into the scope holding the task.</summary>
        [RelayCommand]
        private async Task Unlink()
        {
            if (LinkedTask is not AutomationTask task)
                return;

            Guid scopeId = task.ParentId ?? Scope.Root.Id;
            await MoveAsync(scopeId, $"The interface '{Node.Name}' is no longer linked to the task '{task.Metadata.Name}'.");
        }
    }
}
