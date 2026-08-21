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

        private readonly IScopedService _scoped;

        public WorkflowsViewModel(IScopedService scoped)
        {
            _scoped = scoped;
        }

        public async Task RefreshAsync()
        {
            Roots.Clear();
            foreach (ScopedElement element in await _scoped.GetChildrens(Scope.ROOT_SCOPE_ID))
            {
                var node = new ScopedNode(element, null, _scoped);
                Roots.Add(node);
                await node.LoadAsync();
            }

            Open(Roots.FirstOrDefault());
        }

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
