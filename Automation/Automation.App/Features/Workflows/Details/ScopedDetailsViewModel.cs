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

        /// <summary>
        /// General infos of the element, edited directly by the views : it notifies its own changes.
        /// </summary>
        public ScopedMetadata Metadata => Element.Metadata;

        /// <summary>
        /// Name of the element type, as displayed in the user feedbacks.
        /// </summary>
        protected abstract string TypeName { get; }

        /// <summary>
        /// Whether the metadata has been edited since the last save.
        /// </summary>
        private bool _hasMetadataChanges;

        private readonly WorkflowsViewModel _parent;
        private readonly IScopedService _scoped = SpineViewModel.Instance.Scoped;
        private readonly IOverlayService _overlays = SpineViewModel.Instance.Overlays;
        private readonly IToastService _toasts = SpineViewModel.Instance.Toasts;

        protected ScopedDetailsViewModel(ScopedNode node, WorkflowsViewModel parent)
        {
            Node = node;
            _parent = parent;
            OpenCommand = parent.OpenCommand;

            // The views edit the metadata itself, so its changes are what tells the element needs
            // saving. Tags are edited through the collection rather than the property, hence the
            // second subscription.
            Metadata.PropertyChanged += (_, _) => MarkChanged();
            Metadata.Tags.CollectionChanged += (_, _) => MarkChanged();
        }

        /// <summary>
        /// Save the general infos of the element : its metadata and its own settings.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanSave))]
        public Task Save() => SaveElementAsync($"The {TypeName} '{Node.Name}' has been saved.");

        /// <summary>
        /// Persist the element, then confirm it to the user with [message]. The storage has no partial
        /// update : whichever save is used writes the whole element, so all of them clear every
        /// pending change.
        /// </summary>
        protected async Task SaveElementAsync(string message)
        {
            await _scoped.EditAsync(Element);
            _hasMetadataChanges = false;
            OnSaved();
            SaveCommand.NotifyCanExecuteChanged();
            _toasts.Success(message, $"{TypeName} saved");
        }

        /// <summary>
        /// Records an edit, so <see cref="SaveCommand"/> is only enabled while there is something to
        /// save.
        /// </summary>
        protected void MarkChanged()
        {
            _hasMetadataChanges = true;
            SaveCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Whether <see cref="SaveCommand"/> can currently execute : as soon as the general infos have
        /// been edited. Whatever a page edits besides them (e.g. the workflow graph) gets its own save.
        /// </summary>
        protected virtual bool CanSave => _hasMetadataChanges;

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
