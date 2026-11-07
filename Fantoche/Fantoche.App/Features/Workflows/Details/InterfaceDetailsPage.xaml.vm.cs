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
    /// An interface of a task or a workflow (see <see cref="AutomationInterface"/>) : the class it
    /// points at, and the element it is linked to by hanging under it.
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

        /// <summary>The task or workflow it hangs under, which can show and ask with it ; null while it is linked to none.</summary>
        public BaseAutomationTask? LinkedTask => Node.Parent?.Element as BaseAutomationTask;

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

        /// <summary>
        /// Hang the interface under a task or a workflow : the element then shows and asks with it,
        /// unless its settings pick another one.
        /// </summary>
        [RelayCommand]
        private async Task Link()
        {
            BaseAutomationTask? picked = await TaskSelectionViewModel.ShowAsync();
            if (picked == null)
                return;
            if (picked is AutomationControl)
            {
                _toasts.Warning("A control is built in : it can't be given an interface.", "Pick a task or a workflow");
                return;
            }

            await MoveAsync(picked.Id, $"The interface '{Node.Name}' is linked to '{picked.Metadata.Name}'.");
        }

        /// <summary>Take the interface back from its element, into the scope holding the element.</summary>
        [RelayCommand]
        private async Task Unlink()
        {
            if (LinkedTask is not BaseAutomationTask task)
                return;

            Guid scopeId = task.ParentId ?? Scope.Root.Id;
            await MoveAsync(scopeId, $"The interface '{Node.Name}' is no longer linked to '{task.Metadata.Name}'.");
        }
    }
}
