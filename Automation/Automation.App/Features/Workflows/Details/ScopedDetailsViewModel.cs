using Automation.App.Common;
using Automation.App.Features.Workflows.Details.Controls;
using Automation.Shared.Data.Scoped;
using Automation.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Joufflu.Feedback;
using Joufflu.Navigation;

namespace Automation.App.Features.Workflows.Details
{
    public enum EnumDetailTab
    {
        Settings,
        History,
        Usages,
        Editor
    }

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

        [ObservableProperty]
        private EnumDetailTab _currentTab;

        /// <summary>Executions of the element, displayed by the history tab.</summary>
        public HistoryViewModel History { get; }

        /// <summary>
        /// Graph nodes pointing at the element, displayed by the usages tab of the tasks and of the
        /// workflows : only what runs can be used by a graph, a scope never is.
        /// </summary>
        public UsagesViewModel Usages { get; }

        /// <summary>
        /// General infos of the element, edited directly by the views : it notifies its own changes.
        /// </summary>
        public ScopedMetadata Metadata => Element.Metadata;

        private bool _hasMetadataChanges;

        private readonly WorkflowsViewModel _parent;

        /// <summary>Handed down to whatever the page builds of its own.</summary>
        protected readonly AppServices Services;

        private readonly IScopedService _scoped;
        private readonly IOverlayService _overlays;
        private readonly IToastService _toasts;

        protected ScopedDetailsViewModel(ScopedNode node, WorkflowsViewModel parent, AppServices services)
        {
            Node = node;
            _parent = parent;
            Services = services;
            _scoped = services.Scoped;
            _overlays = services.Overlays;
            _toasts = services.Toasts;
            OpenCommand = parent.OpenCommand;
            History = new HistoryViewModel(node, services.History);
            Usages = new UsagesViewModel(node, parent, services.Scoped);

            // The views edit the metadata itself, so its changes are what tells the element needs
            // saving. Tags are edited through the collection rather than the property, hence the
            // second subscription.
            Metadata.PropertyChanged += (_, _) => MarkChanged();
            Metadata.Tags.CollectionChanged += (_, _) => MarkChanged();
        }

        /// <summary>Save the general infos of the element : its metadata and its own settings.</summary>
        [RelayCommand(CanExecute = nameof(CanSave))]
        public Task Save() => SaveElementAsync($"The {Node.Type} '{Node.Name}' has been saved.");

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
            _toasts.Success(message, $"{Node.Type} saved");
        }

        /// <summary>Record an edit, so <see cref="SaveCommand"/> is only enabled while there is one.</summary>
        protected void MarkChanged()
        {
            _hasMetadataChanges = true;
            SaveCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Whatever a page edits besides the general infos (e.g. the workflow graph) gets its own
        /// save, so this one only follows them.
        /// </summary>
        protected virtual bool CanSave => _hasMetadataChanges;

        /// <summary>Called once the element has been saved, for whatever the page has to reset.</summary>
        protected virtual void OnSaved()
        { }

        partial void OnCurrentTabChanged(EnumDetailTab value)
        {
            if (value == EnumDetailTab.Usages)
                _ = Usages.RefreshAsync();
        }

        [RelayCommand(CanExecute = nameof(CanDelete))]
        public async Task Delete()
        {
            if (await _overlays.Confirm($"Are you sure you want to delete the {Node.Type} '{Node.Name}' ?", "Confirm deletion", EnumConfirmationType.Danger) != true)
                return;

            try
            {
                await _scoped.RemoveAsync(Element);
            }
            catch (InvalidOperationException exception)
            {
                // What is read only, or what a graph still has a node pointing at, stays.
                _toasts.Error(exception.Message, $"The {Node.Type} '{Node.Name}' could not be deleted");
                return;
            }

            _parent.Remove(Node);
            // Fall back on the parent scope, the element not being displayable anymore
            _parent.Open(Node.Parent);
            _toasts.Success($"The {Node.Type} '{Node.Name}' has been deleted.", $"{Node.Type} deleted");
        }

        /// <summary>
        /// The built-in elements (e.g. the control tasks every graph relies on) are read only.
        /// </summary>
        protected bool CanDelete => !Metadata.IsReadOnly;
    }
}
