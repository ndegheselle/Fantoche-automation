using Automation.Shared.Base;
using Automation.Shared.Data.Scoped;
using Automation.Shared.Services;
using Automation.App.Features.Workflows.Details;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Automation.App.Features.Workflows
{
    public partial class WorkflowsViewModel : ObservableObject
    {
        public ObservableCollection<ScopedNode> Roots { get; } = [];

        /// <summary>
        /// Path of the selected element, used by the breadcrumb.
        /// </summary>
        public IEnumerable<ScopedNode> Breadcrumb => Selected?.Path ?? [];

        [ObservableProperty] private ScopedNode? _selected;
        [ObservableProperty] private object? _details;
        [ObservableProperty] private string _search = "";

        /// <summary>
        /// Whether the tree is currently being (re)loaded, the views showing a loader meanwhile.
        /// </summary>
        [ObservableProperty] private bool _isLoading;

        /// <summary>
        /// Cancellation of the running refresh : typing in the search starts one per keystroke, and
        /// only the last one is allowed to display its result.
        /// </summary>
        private CancellationTokenSource? _refreshCancellation;

        private readonly IScopedService _scoped;

        public WorkflowsViewModel(IScopedService scoped)
        {
            _scoped = scoped;
        }

        public async Task RefreshAsync()
        {
            _refreshCancellation?.Cancel();
            using var cancellation = new CancellationTokenSource();
            _refreshCancellation = cancellation;
            IsLoading = true;

            try
            {
                // Loaded aside from the displayed tree, which is only replaced once everything is
                // there : no flickering, and a refresh made obsolete by a newer one changes nothing.
                List<ScopedNode> roots = await LoadRootsAsync();
                if (cancellation.IsCancellationRequested)
                    return;

                Roots.Clear();
                foreach (ScopedNode root in roots)
                    Roots.Add(root);

                Open(Roots.FirstOrDefault());
            }
            finally
            {
                if (_refreshCancellation == cancellation)
                {
                    _refreshCancellation = null;
                    IsLoading = false;
                }
            }
        }

        /// <summary>
        /// Elements displayed at the root of the tree : the content of the root scope, or the results
        /// of the search when there is one.
        /// </summary>
        private async Task<List<ScopedNode>> LoadRootsAsync()
        {
            List<ScopedNode> roots = [];

            if (string.IsNullOrWhiteSpace(Search))
            {
                foreach (ScopedElement element in await _scoped.GetChildrensAsync(Scope.ROOT_SCOPE_ID))
                {
                    var node = new ScopedNode(element, null, _scoped);
                    roots.Add(node);
                    await node.LoadAsync();
                }
            }
            else
            {
                // Flat list of the first page of tasks and workflows matching the search, scopes are not searchable.
                Paginated<BaseAutomationTask> page = await _scoped.SearchAsync(Search);
                foreach (BaseAutomationTask element in page.Items)
                    roots.Add(new ScopedNode(element, null, _scoped));
            }

            return roots;
        }

        partial void OnSearchChanged(string value) => _ = RefreshAsync();

        /// <summary>
        /// Create a new element of [type] in the selected scope. When the selection is not a scope
        /// its parent scope is used, defaulting to the root scope.
        /// </summary>
        [RelayCommand]
        public async Task Create(EnumScopedType type)
        {
            ScopedNode? parent = Selected?.IsScope == true ? Selected : Selected?.Parent;
            Guid parentId = parent?.Element.Id ?? Scope.ROOT_SCOPE_ID;
            string name = await GetAvailableNameAsync(parentId, type);

            ScopedElement element = type switch
            {
                EnumScopedType.Scope => new Scope(name, parentId),
                EnumScopedType.Workflow => new AutomationWorkflow(name, parentId),
                EnumScopedType.Task => new AutomationTask(name, parentId),
                _ => throw new NotSupportedException($"Unknown scoped type '{type}'")
            };

            var node = new ScopedNode(await _scoped.CreateAsync(element), parent, _scoped);
            (parent?.Children ?? Roots).Add(node);
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
