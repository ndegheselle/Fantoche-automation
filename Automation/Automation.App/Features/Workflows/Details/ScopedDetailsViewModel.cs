using Automation.Shared.Data.Scoped;
using Automation.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Joufflu.Feedback.Controls;
using Joufflu.Navigation;

namespace Automation.App.Features.Workflows.Details
{
    /// <summary>
    /// Base of the details pages view models, handling what every scoped element shares : its name
    /// and the save / delete actions.
    /// </summary>
    /// <typeparam name="TElement">Type of the displayed element.</typeparam>
    public abstract partial class ScopedDetailsViewModel<TElement> : ObservableObject
        where TElement : ScopedElement
    {
        public ScopedNode Node { get; }
        public TElement Element => (TElement)Node.Element;
        public IRelayCommand<ScopedNode?> OpenCommand { get; }

        public string Name
        {
            get => Element.Metadata.Name;
            set
            {
                Element.Metadata.Name = value;
                OnPropertyChanged();
                Node.NotifyNameChanged();
            }
        }

        /// <summary>
        /// Name of the element type, as displayed in the user feedbacks.
        /// </summary>
        protected abstract string TypeName { get; }

        private readonly WorkflowsViewModel _parent;
        private readonly IScopedService _scoped = SpineViewModel.Instance.Scoped;
        private readonly IOverlayService _overlays = SpineViewModel.Instance.Overlays;
        private readonly IToastService _toasts = SpineViewModel.Instance.Toasts;

        protected ScopedDetailsViewModel(ScopedNode node, WorkflowsViewModel parent)
        {
            Node = node;
            _parent = parent;
            OpenCommand = parent.OpenCommand;
        }

        [RelayCommand]
        public async Task Save()
        {
            await _scoped.EditAsync(Element);
            OnSaved();
            _toasts.Success($"The {TypeName} '{Node.Name}' has been saved.", $"{TypeName} saved");
        }

        /// <summary>
        /// Called once the element has been saved, for whatever has to be reset by the page.
        /// </summary>
        protected virtual void OnSaved()
        { }

        [RelayCommand]
        public async Task Delete()
        {
            if (await _overlays.Confirm($"Are you sure you want to delete the {TypeName} '{Node.Name}' ?", "Confirm deletion", EnumConfirmationType.Danger) != true)
                return;

            await _scoped.RemoveAsync(Element);
            Node.Parent?.Children.Remove(Node);
            // Fall back on the parent scope, the element not being displayable anymore
            _parent.Open(Node.Parent);
        }
    }
}
