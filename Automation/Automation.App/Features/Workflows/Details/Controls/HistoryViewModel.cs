using System.Collections.ObjectModel;
using Automation.App.Common;
using Automation.Shared.Base;
using Automation.Shared.Data.Execution;
using Automation.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Automation.App.Features.Workflows.Details.Controls
{
    /// <summary>
    /// Execution history of a scoped element : its own instances for a task or a workflow, the ones
    /// of every task and workflow it contains for a scope. While it is displayed it follows the
    /// executions live, the history service reporting every instance as it is created and changes.
    /// </summary>
    public partial class HistoryViewModel : ObservableObject
    {
        /// <summary>Displayed page of instances, most recent first.</summary>
        public ObservableCollection<TaskInstance> Instances { get; } = [];

        public PagingViewModel Paging { get; }

        [ObservableProperty] private long _total;

        private readonly ScopedNode _node;
        private readonly IHistoryService _history;

        /// <summary>
        /// Whether the realtime events are currently listened to, so a displayed page isn't
        /// subscribed twice.
        /// </summary>
        private bool _isSubscribed;

        public HistoryViewModel(ScopedNode node, IHistoryService history)
        {
            _node = node;
            _history = history;
            Paging = new PagingViewModel(RefreshAsync);
        }

        /// <summary>
        /// Start following the executions and load the first page, the history being displayed.
        /// </summary>
        public async Task SubscribeAsync()
        {
            if (!_isSubscribed)
            {
                _history.InstanceAdded += OnInstanceAdded;
                _history.InstanceUpdated += OnInstanceUpdated;
                _isSubscribed = true;
            }

            await RefreshAsync();
        }

        /// <summary>Stop following the executions, the history not being displayed anymore.</summary>
        public void Unsubscribe()
        {
            if (!_isSubscribed)
                return;

            _history.InstanceAdded -= OnInstanceAdded;
            _history.InstanceUpdated -= OnInstanceUpdated;
            _isSubscribed = false;
        }

        /// <summary>
        /// Open the detail of [instance] : what it ran with and produced, and the nodes it ran when
        /// it is a workflow.
        /// </summary>
        [RelayCommand]
        private Task OpenDetail(TaskInstance? instance)
            => instance == null ? Task.CompletedTask : InstanceDetailViewModel.ShowAsync(instance);

        public async Task RefreshAsync()
        {
            var page = await _history.GetByScopedAsync(_node.Element.Id, Paging.Options);

            Instances.Clear();
            foreach (var instance in page.Items)
                Instances.Add(instance);
            Total = page.Total;
        }

        #region Realtime
        private void OnInstanceAdded(TaskInstance instance)
        {
            Ui.Dispatch(() =>
            {
                // Only the first page shows the executions as they start, the following ones holding
                // older instances that a new one doesn't belong to.
                if (Paging.PageNumber != 1)
                    return;

                // A scope covers every task and workflow under it, which only the service knows :
                // the tree can be displaying part of itself (a search) and the branch is walked in
                // SQL. So the page is read again rather than the instance being placed by hand.
                if (_node.IsScope)
                {
                    _ = RefreshAsync();
                    return;
                }

                if (instance.TaskId != _node.Element.Id)
                    return;

                Total++;
                Instances.Insert(0, instance);
                while (Instances.Count > Paging.Capacity)
                    Instances.RemoveAt(Instances.Count - 1);
            });
        }

        private void OnInstanceUpdated(TaskInstance instance)
        {
            Ui.Dispatch(() =>
            {
                // TaskInstance doesn't notify its own changes : the row is replaced so the displayed
                // state follows the execution.
                for (int index = 0; index < Instances.Count; index++)
                {
                    if (Instances[index].Id != instance.Id)
                        continue;

                    Instances[index] = instance;
                    return;
                }
            });
        }
        #endregion
    }
}
