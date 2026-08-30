using System.Collections.ObjectModel;
using System.Windows;
using Automation.Shared.Base;
using Automation.Shared.Data.Execution;
using Automation.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Automation.App.Features.Workflows.Details.Controls
{
    /// <summary>
    /// Execution history of a scoped element : its own instances for a task or a workflow, the ones of
    /// every task and workflow it contains for a scope.
    /// While it is displayed it follows the executions live, the history service reporting every
    /// instance as it is created and as it changes.
    /// </summary>
    public partial class HistoryViewModel : ObservableObject
    {
        /// <summary>
        /// Displayed page of instances, most recent first.
        /// </summary>
        public ObservableCollection<TaskInstance> Instances { get; } = [];

        [ObservableProperty] private long _total;
        [ObservableProperty] private int _pageNumber = 1;
        [ObservableProperty] private int _capacity = 50;

        private readonly ScopedNode _node;
        private readonly IScopedService _scoped;
        private readonly IHistoryService _history;

        /// <summary>
        /// Whether the realtime events are currently listened to, so a displayed page isn't
        /// subscribed twice.
        /// </summary>
        private bool _isSubscribed;

        public HistoryViewModel(ScopedNode node, IScopedService scoped, IHistoryService history)
        {
            _node = node;
            _scoped = scoped;
            _history = history;
        }

        /// <summary>
        /// Start following the executions and load the first page. Called when the history is
        /// displayed.
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

        /// <summary>
        /// Stop following the executions. Called when the history is not displayed anymore.
        /// </summary>
        public void Unsubscribe()
        {
            if (!_isSubscribed)
                return;

            _history.InstanceAdded -= OnInstanceAdded;
            _history.InstanceUpdated -= OnInstanceUpdated;
            _isSubscribed = false;
        }

        public async Task RefreshAsync()
        {
            var page = await _scoped.GetHistoryAsync(
                _node.Element.Id,
                new PaginationOptions() { Page = PageNumber, PageSize = Capacity });

            Instances.Clear();
            foreach (var instance in page.Items)
                Instances.Add(instance);
            Total = page.Total;
        }

        partial void OnCapacityChanged(int value) => _ = RefreshAsync();

        partial void OnPageNumberChanged(int value) => _ = RefreshAsync();

        #region Realtime
        /// <summary>
        /// The ids whose executions belong to this history : the element itself, or every task and
        /// workflow nested under it when it is a scope.
        /// </summary>
        private bool IsDisplayed(TaskInstance instance)
        {
            if (!_node.IsScope)
                return instance.TaskId == _node.Element.Id;
            return _node.Descendants.Any(x => x.TaskElement != null && x.TaskElement.Id == instance.TaskId);
        }

        private void OnInstanceAdded(TaskInstance instance)
        {
            // The instances are reported by the threads running the executions.
            Dispatch(() =>
            {
                if (!IsDisplayed(instance))
                    return;

                Total++;
                // Only the first page shows the executions as they start, the following ones holding
                // older instances that a new one doesn't belong to.
                if (PageNumber != 1)
                    return;

                Instances.Insert(0, instance);
                while (Instances.Count > Capacity)
                    Instances.RemoveAt(Instances.Count - 1);
            });
        }

        private void OnInstanceUpdated(TaskInstance instance)
        {
            Dispatch(() =>
            {
                // TaskInstance doesn't notify its own changes : the row is replaced so the displayed
                // state follows the execution.
                int index = Instances.ToList().FindIndex(x => x.Id == instance.Id);
                if (index >= 0)
                    Instances[index] = instance;
            });
        }

        private static void Dispatch(Action action)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
                action();
            else
                dispatcher.Invoke(action);
        }
        #endregion
    }
}
