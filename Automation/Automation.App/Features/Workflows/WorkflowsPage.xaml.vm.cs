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
        /// Whether a search is running, the tree and its results not being displayed together.
        /// </summary>
        public bool IsSearching => !string.IsNullOrWhiteSpace(Search);

        /// <summary>
        /// Path of the selected element, used by the breadcrumb.
        /// </summary>
        public IEnumerable<ScopedNode> Breadcrumb => Selected?.Path ?? [];

        [ObservableProperty] private ScopedNode? _selected;

        /// <summary>
        /// Result highlighted in the search list, the page opening it once the press it comes from
        /// turns out to be a click rather than a drag.
        /// </summary>
        [ObservableProperty] private ScopedNode? _selectedResult;

        [ObservableProperty] private object? _details;
        [ObservableProperty] private string _search = "";

        private readonly IScopedService _scoped;

        public WorkflowsViewModel(IScopedService scoped)
        {
            _scoped = scoped;
            Root = new ScopedNode(Scope.Root, null, scoped);
        }

        public async Task RefreshAsync()
        {
            await Root.LoadAsync();
            Open(Root.Children.FirstOrDefault());
        }

        /// <summary>
        /// Fill <see cref="SearchResults"/> with the first page of tasks and workflows matching
        /// <see cref="Search"/>, scopes not being searchable.
        /// </summary>
        private async Task SearchAsync()
        {
        }

        partial void OnSearchChanged(string value)
        {
            OnPropertyChanged(nameof(IsSearching));
            _ = SearchAsync();
        }

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

            ScopedElement element = type switch
            {
                EnumScopedType.Scope => new Scope(name, parentId),
                EnumScopedType.Workflow => new AutomationWorkflow(name, parentId),
                EnumScopedType.Task => new AutomationTask(name, parentId),
                _ => throw new NotSupportedException($"Unknown scoped type '{type}'")
            };

            var node = new ScopedNode(await _scoped.CreateAsync(element), parent, _scoped);
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

        /// <summary>
        /// Drop [node] and whatever it contains from the tree, once they have been deleted.
        /// </summary>
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
                AutomationWorkflow => new WorkflowDetailsViewModel(value, this),
                AutomationTask => new TaskDetailsViewModel(value, this),
                Scope => new ScopeDetailsViewModel(value, this),
                _ => null
            };
        }
    }
}
