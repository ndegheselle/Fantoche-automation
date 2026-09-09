using Automation.App.Common;
using Automation.App.Features.Workflows.Details;
using Automation.Shared.Data.Scoped;
using Automation.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Automation.App.Features.Workflows
{
    public partial class WorkflowsViewModel : ObservableObject
    {
        /// <summary>
        /// Scope holding the whole hierarchy. It is only a container : the tree displays its children.
        /// </summary>
        public ScopedNode Root { get; }

        /// <summary>
        /// Path of the selected element, used by the breadcrumb.
        /// </summary>
        public IEnumerable<ScopedNode> Breadcrumb => Selected?.Path ?? [];

        [ObservableProperty] private ScopedNode? _selected;

        [ObservableProperty] private object? _details;
        [ObservableProperty] private string _search = "";

        /// <summary>Handed down to the details of whatever is selected.</summary>
        private readonly AppServices _services;

        private readonly IScopedService _scoped;

        public WorkflowsViewModel(AppServices services)
        {
            _services = services;
            _scoped = services.Scoped;
            Root = new ScopedNode(Scope.Root, null);
        }

        public async Task RefreshAsync()
        {
            Fill(await _scoped.GetTreeAsync());
            Open(Root.Children.FirstOrDefault());
        }

        /// <summary>
        /// Rebuild the tree with the tasks and workflows matching <see cref="Search"/> and the scopes
        /// leading to them, scopes not being searchable. An empty search restores the whole tree.
        /// </summary>
        private async Task SearchAsync()
        {
            string search = Search;
            bool isSearching = !string.IsNullOrWhiteSpace(search);

            List<ScopedElement> elements = isSearching
                ? await _scoped.SearchTreeAsync(search)
                : await _scoped.GetTreeAsync();

            // Another search may have been typed while this one was running : only the last one wins.
            if (search != Search)
                return;

            Fill(elements);

            // The results are buried in their scopes otherwise.
            if (isSearching)
                Root.ExpandAll();
        }

        /// <summary>
        /// Hang [elements] under the root by their parent. The root is the tree container and is not
        /// one of them : its own children hang directly under it.
        /// </summary>
        private void Fill(List<ScopedElement> elements)
            => Root.Load(elements.Where(x => x.Id != Root.Element.Id).ToLookup(x => x.ParentId));

        partial void OnSearchChanged(string value) => _ = SearchAsync();

        /// <summary>
        /// Create a new element of [type] in the selected scope. When the selection is not a scope
        /// its parent scope is used, defaulting to the root scope.
        /// </summary>
        [RelayCommand]
        public async Task Create(EnumScopedType type)
        {
            ScopedNode selected = Selected ?? Root;
            ScopedNode parent = selected.IsScope ? selected : selected.Parent ?? Root;
            Guid parentId = parent.Element.Id;
            string name = await GetAvailableNameAsync(parentId, type);

            ScopedElement element = ScopedElement.Create(type, name, parentId);
            var node = new ScopedNode(await _scoped.CreateAsync(element), parent);
            parent.Children.Add(node);
            Open(node);
        }

        /// <summary>
        /// First name not already taken by a sibling, the name having to be unique within a scope.
        /// </summary>
        private async Task<string> GetAvailableNameAsync(Guid parentId, EnumScopedType type)
        {
            string name = $"New {type.ToString().ToLower()}";
            for (int index = 2; !await _scoped.IsNameUniqueAsync(parentId, name); index++)
                name = $"New {type.ToString().ToLower()} {index}";
            return name;
        }

        public void Remove(ScopedNode node)
        {
            node.Parent?.Children.Remove(node);
        }

        [RelayCommand]
        public void Open(ScopedNode? node)
        {
            if (node == null)
                return;

            foreach (ScopedNode ancestor in node.Path)
                ancestor.IsExpanded = true;
            node.IsSelected = true;
            Selected = node;
        }

        partial void OnSelectedChanged(ScopedNode? value)
        {
            OnPropertyChanged(nameof(Breadcrumb));
            Details = value?.Element switch
            {
                AutomationWorkflow => new WorkflowDetailsViewModel(value, this, _services),
                AutomationTask => new TaskDetailsViewModel(value, this, _services),
                Scope => new ScopeDetailsViewModel(value, this, _services),
                _ => null
            };
        }
    }
}
