using System.Collections.ObjectModel;
using Automation.Shared.Data.Scoped;
using Automation.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Automation.App.Features.Workflows.Details.Controls
{
    /// <summary>
    /// What a task or a workflow is used by : the graph nodes pointing at it, each with the workflow
    /// holding them. Read when the usages are displayed : a graph edited elsewhere is not reported,
    /// so the list is loaded again every time the tab is shown.
    /// </summary>
    public partial class UsagesViewModel : ObservableObject
    {
        /// <summary>
        /// Nodes using the element, grouped by the workflow they belong to.
        /// </summary>
        public ObservableCollection<TaskUsage> Usages { get; } = [];

        /// <summary>
        /// Whether the element is used nowhere, which is also what makes it deletable.
        /// </summary>
        public bool IsEmpty => Usages.Count == 0;

        private readonly ScopedNode _node;
        private readonly WorkflowsViewModel _parent;
        private readonly IScopedService _scoped;

        public UsagesViewModel(ScopedNode node, WorkflowsViewModel parent, IScopedService scoped)
        {
            _node = node;
            _parent = parent;
            _scoped = scoped;
        }

        public async Task RefreshAsync()
        {
            List<TaskUsage> usages = await _scoped.GetUsagesAsync(_node.Element.Id);

            Usages.Clear();
            foreach (TaskUsage usage in usages.OrderBy(x => x.WorkflowName).ThenBy(x => x.NodeName))
                Usages.Add(usage);
            OnPropertyChanged(nameof(IsEmpty));
        }

        /// <summary>
        /// Open the workflow [usage] belongs to, so the node using the element can be looked at in
        /// its graph. A workflow the tree doesn't currently hold (e.g. while a search only displays
        /// part of it) is left alone.
        /// </summary>
        [RelayCommand]
        private void OpenWorkflow(TaskUsage? usage)
        {
            if (usage == null)
                return;

            _parent.Open(_parent.Find(usage.WorkflowId));
        }
    }
}
