using System.Collections.ObjectModel;
using System.Windows;
using Automation.Shared.Data.Execution;
using Automation.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Automation.App.Features.Workers
{
    /// <summary>
    /// Observable wrapper around a <see cref="WorkerInfos"/> so the grid reflects the load / state
    /// changes pushed by <see cref="IWorkersService.WorkerUpdated"/> in real time.
    /// </summary>
    public class WorkerRowViewModel : ObservableObject
    {
        public string Id { get; }

        private string _name = string.Empty;
        public string Name { get => _name; private set => SetProperty(ref _name, value); }

        private string _machineName = string.Empty;
        public string MachineName { get => _machineName; private set => SetProperty(ref _machineName, value); }

        private EnumWorkerState _state;
        public EnumWorkerState State { get => _state; private set => SetProperty(ref _state, value); }

        private string _load = string.Empty;
        public string Load { get => _load; private set => SetProperty(ref _load, value); }

        private string _queue = string.Empty;
        public string Queue { get => _queue; private set => SetProperty(ref _queue, value); }

        private string _version = string.Empty;
        public string Version { get => _version; private set => SetProperty(ref _version, value); }

        private DateTime _lastHeartbeat;
        public DateTime LastHeartbeat { get => _lastHeartbeat; private set => SetProperty(ref _lastHeartbeat, value); }

        public WorkerRowViewModel(WorkerInfos worker)
        {
            Id = worker.Id;
            Update(worker);
        }

        public void Update(WorkerInfos worker)
        {
            Name = worker.Name;
            MachineName = worker.MachineName;
            State = worker.State;
            Load = $"{worker.RunningTasks} / {worker.MaxParallelTasks}";
            Queue = $"{worker.QueuedTasks} / {worker.MaxQueueSize}";
            Version = worker.Version;
            LastHeartbeat = worker.LastHeartbeat.ToLocalTime();
        }
    }

    public class WorkersViewModel : ObservableObject
    {
        private readonly IWorkersService _workers;

        public ObservableCollection<WorkerRowViewModel> Workers { get; } = [];

        public IAsyncRelayCommand RefreshCommand { get; }

        public WorkersViewModel(IWorkersService workers)
        {
            _workers = workers;
            RefreshCommand = new AsyncRelayCommand(LoadAsync);

            _workers.WorkerUpdated += OnWorkerUpdated;
            _workers.WorkerRemoved += OnWorkerRemoved;

            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            var workers = await _workers.GetWorkersAsync();
            Workers.Clear();
            foreach (var worker in workers.OrderBy(x => x.Name))
                Workers.Add(new WorkerRowViewModel(worker));
        }

        private void OnWorkerUpdated(WorkerInfos worker)
        {
            Dispatch(() =>
            {
                var row = Workers.FirstOrDefault(x => x.Id == worker.Id);
                if (row != null)
                    row.Update(worker);
                else
                    Workers.Add(new WorkerRowViewModel(worker));
            });
        }

        private void OnWorkerRemoved(string workerId)
        {
            Dispatch(() =>
            {
                var row = Workers.FirstOrDefault(x => x.Id == workerId);
                if (row != null)
                    Workers.Remove(row);
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
    }
}
