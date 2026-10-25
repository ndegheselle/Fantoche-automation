using Fantoche.App.Common;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Scoped;
using Fantoche.Shared.Remote;
using Fantoche.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Fantoche.App.Features.Workers
{
    /// <summary>
    /// The worker of the application, as the supervisor knows it : [DispatchId] is the id it was
    /// added to the workers of the supervisor under (for the tasks asking the user, and the runs too
    /// in standalone mode), null when it isn't one of them.
    /// </summary>
    public record ApplicationWorker(string? DispatchId);

    /// <param name="Capacity">Null for the application : what it starts itself isn't limited.</param>
    public record WorkerRow(string Id, int Running, int? Capacity)
    {
        public string Load => Capacity == null ? $"{Running}" : $"{Running} / {Capacity}";
    }

    /// <summary>An execution running on [Worker], or waiting for one when it is null.</summary>
    public record ExecutionRow(string? Worker, string TaskName, DateTime CreatedAt);

    /// <summary>
    /// The workers and what they run : the application first, with the executions it started
    /// itself, then the workers of the supervisor and what waits for room on one. Followed as the
    /// application and the supervisor tell their changes.
    /// </summary>
    public partial class WorkersViewModel : ObservableObject
    {
        public const string ApplicationName = "This application";

        [ObservableProperty] private List<WorkerRow> _workers = [];
        [ObservableProperty] private List<ExecutionRow> _running = [];
        [ObservableProperty] private List<ExecutionRow> _pending = [];

        private readonly IDispatchService _dispatch;
        private readonly IExecutionService _execution;
        private readonly IScopedService _scoped;
        private readonly ApplicationWorker _application;

        /// <summary>The page can be shown again before it was hidden : never subscribed twice.</summary>
        private bool _isSubscribed;

        /// <summary>The last status of the supervisor, shown again along with the application's changes.</summary>
        private DispatchStatus _dispatchStatus = new([], []);

        /// <summary>
        /// Counts the changes shown : the names are read before a change is shown, and a newer one
        /// may have arrived in the meantime.
        /// </summary>
        private int _lastChange;

        public WorkersViewModel(IDispatchService dispatch, IExecutionService execution, IScopedService scoped, ApplicationWorker application)
        {
            _dispatch = dispatch;
            _execution = execution;
            _scoped = scoped;
            _application = application;
        }

        /// <summary>Start following the workers and read them, the page being displayed.</summary>
        public async Task SubscribeAsync()
        {
            if (!_isSubscribed)
            {
                _dispatch.StatusChanged += OnStatusChanged;
                _execution.RunningChanged += OnRunningChanged;
                _isSubscribed = true;
            }

            // Shown alone first : the application is there even when the supervisor isn't reachable.
            await ShowAsync();
            _dispatchStatus = await _dispatch.GetStatusAsync();
            await ShowAsync();
        }

        /// <summary>Stop following the workers, the page not being displayed anymore.</summary>
        public void Unsubscribe()
        {
            if (!_isSubscribed)
                return;

            _dispatch.StatusChanged -= OnStatusChanged;
            _execution.RunningChanged -= OnRunningChanged;
            _isSubscribed = false;
        }

        private void OnStatusChanged(DispatchStatus status) => Ui.Post(() =>
        {
            _dispatchStatus = status;
            _ = ShowAsync();
        });

        private void OnRunningChanged() => Ui.Post(() => _ = ShowAsync());

        private async Task ShowAsync()
        {
            int current = ++_lastChange;
            DispatchStatus status = _dispatchStatus;
            IReadOnlyCollection<TaskInstance> local = _execution.GetRunning();

            // In standalone mode the application is also a worker of the supervisor : one row.
            WorkerStatus? dispatched = status.Workers.FirstOrDefault(x => x.Id == _application.DispatchId);
            List<WorkerStatus> others = [.. status.Workers.Where(x => x != dispatched)];

            List<(string Worker, Guid TaskId, DateTime CreatedAt)> running =
            [
                .. local.Select(x => (ApplicationName, x.TaskId, x.CreatedAt)),
                .. (dispatched?.Running ?? []).Select(x => (ApplicationName, x.TaskId, x.CreatedAt)),
                .. others.SelectMany(worker => worker.Running.Select(x => (worker.Id, x.TaskId, x.CreatedAt))),
            ];
            var taskIds = running.Select(x => x.TaskId).Concat(status.Pending.Select(x => x.TaskId));
            var names = await GetNamesAsync(taskIds);
            if (current != _lastChange)
                return;

            int applicationRunning = local.Count + (dispatched?.Running.Count ?? 0);
            Workers =
            [
                new WorkerRow(ApplicationName, applicationRunning, null),
                .. others.Select(x => new WorkerRow(x.Id, x.Running.Count, x.Capacity)),
            ];
            Running = [.. running.Select(x => new ExecutionRow(x.Worker, names[x.TaskId], x.CreatedAt))];
            Pending = [.. status.Pending.Select(x => new ExecutionRow(null, names[x.TaskId], x.CreatedAt))];
        }

        /// <summary>
        /// The names of [taskIds], read in one go. An execution can outlive its task : a removed one
        /// is still listed, under its id.
        /// </summary>
        private async Task<Dictionary<Guid, string>> GetNamesAsync(IEnumerable<Guid> taskIds)
        {
            List<Guid> ids = [.. taskIds.Distinct()];
            if (ids.Count == 0)
                return [];

            List<ScopedElement> elements = await _scoped.GetAsync(ids);
            Dictionary<Guid, string> found = elements.ToDictionary(x => x.Id, x => x.Metadata.Name);
            return ids.ToDictionary(id => id, id => found.GetValueOrDefault(id) ?? $"Removed ({id:N})");
        }
    }
}
