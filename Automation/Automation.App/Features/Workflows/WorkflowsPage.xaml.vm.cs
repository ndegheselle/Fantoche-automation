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
                AutomationTask => new TaskDetailsViewModel(value),
                Scope => new ScopeDetailsViewModel(value, this),
                _ => null
            };
        }
    }
}
