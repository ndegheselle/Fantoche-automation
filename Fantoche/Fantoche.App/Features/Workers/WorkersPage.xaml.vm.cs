using Fantoche.App.Common;
using Fantoche.Shared.Remote;
using Fantoche.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Fantoche.App.Features.Workers
{
    public record WorkerRow(string Id, int Running, int Capacity)
    {
        public string Load => $"{Running} / {Capacity}";
    }

    /// <summary>An execution running on [Worker], or waiting for one when it is null.</summary>
    public record ExecutionRow(string? Worker, string TaskName, DateTime CreatedAt);

    /// <summary>
    /// What the supervisor dispatches : its workers, what they run and what waits for room on one,
    /// followed as the supervisor pushes its changes.
    /// </summary>
    public partial class WorkersViewModel : ObservableObject
    {
        [ObservableProperty] private List<WorkerRow> _workers = [];
        [ObservableProperty] private List<ExecutionRow> _running = [];
        [ObservableProperty] private List<ExecutionRow> _pending = [];

        private readonly IDispatchService _dispatch;
        private readonly IScopedService _scoped;

        /// <summary>The page can be shown again before it was hidden : never subscribed twice.</summary>
        private bool _isSubscribed;

        /// <summary>
        /// Counts the statuses received : the names of a status are read before it is shown, and a
        /// newer one may have arrived in the meantime.
        /// </summary>
        private int _lastStatus;

        public WorkersViewModel(IDispatchService dispatch, IScopedService scoped)
        {
            _dispatch = dispatch;
            _scoped = scoped;
        }

        /// <summary>Start following the dispatch and read it, the page being displayed.</summary>
        public async Task SubscribeAsync()
        {
            if (!_isSubscribed)
            {
                _dispatch.StatusChanged += OnStatusChanged;
                _isSubscribed = true;
            }

            await RefreshAsync();
        }

        /// <summary>Stop following the dispatch, the page not being displayed anymore.</summary>
        public void Unsubscribe()
        {
            if (!_isSubscribed)
                return;

            _dispatch.StatusChanged -= OnStatusChanged;
            _isSubscribed = false;
        }

        private async Task RefreshAsync()
        {
            DispatchStatus status = await _dispatch.GetStatusAsync();
            await ShowAsync(status);
        }

        private void OnStatusChanged(DispatchStatus status) => Ui.Post(() => _ = ShowAsync(status));

        private async Task ShowAsync(DispatchStatus status)
        {
            int current = ++_lastStatus;
            var running = status.Workers.SelectMany(worker => worker.Running.Select(execution => (Worker: worker.Id, Execution: execution))).ToList();
            var taskIds = running.Select(x => x.Execution.TaskId).Concat(status.Pending.Select(x => x.TaskId));
            var names = await TaskNames.ResolveAsync(_scoped, taskIds);
            if (current != _lastStatus)
                return;

            Workers = [.. status.Workers.Select(x => new WorkerRow(x.Id, x.Running.Count, x.Capacity))];
            Running = [.. running.Select(x => new ExecutionRow(x.Worker, names[x.Execution.TaskId].Name, x.Execution.CreatedAt))];
            Pending = [.. status.Pending.Select(x => new ExecutionRow(null, names[x.TaskId].Name, x.CreatedAt))];
        }
    }
}
