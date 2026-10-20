using System.Net.Http;
using Fantoche.Services.Remote;
using Fantoche.Shared.Remote;
using Fantoche.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Fantoche.App.Features.Workers
{
    public enum EnumWorkerState
    {
        Idle,
        Working,
        Full,
    }

    /// <summary>
    /// A worker as the page lists it, the application being one of its own : it runs the tasks set
    /// to run locally.
    /// </summary>
    /// <param name="Load">How many executions it runs, out of how many it can.</param>
    /// <param name="Queue">The executions it runs, which never wait on it : a worker is only handed what it has room for.</param>
    public record WorkerItem(string Name, string Description, bool IsLocal, EnumWorkerState State, string Load, IReadOnlyList<ExecutionSummary> Queue)
    {
        public static WorkerItem Local(IReadOnlyList<ExecutionSummary> running)
        {
            EnumWorkerState state = running.Count == 0 ? EnumWorkerState.Idle : EnumWorkerState.Working;
            return new WorkerItem("Local", "This application", true, state, $"{running.Count} running", running);
        }

        public static WorkerItem Remote(WorkerInfo worker)
        {
            EnumWorkerState state = worker.Running.Count switch
            {
                0 => EnumWorkerState.Idle,
                int count when count >= worker.Capacity => EnumWorkerState.Full,
                _ => EnumWorkerState.Working,
            };
            return new WorkerItem(worker.Id, "Worker of the supervisor", false, state, $"{worker.Running.Count} / {worker.Capacity} running", worker.Running);
        }
    }

    public partial class WorkersViewModel : ObservableObject
    {
        [ObservableProperty] private IReadOnlyList<WorkerItem> _workers = [];

        /// <summary>The executions waiting for a worker of the supervisor to have room.</summary>
        [ObservableProperty] private IReadOnlyList<ExecutionSummary> _waiting = [];

        /// <summary>Why the workers of the supervisor aren't listed, null when they are.</summary>
        [ObservableProperty] private string? _supervisorError;

        /// <summary>Only the application runs the tasks : there is no supervisor and no queue.</summary>
        public bool IsStandalone => _supervisor == null;

        private readonly IExecutionService _execution;
        private readonly RemoteWorkersService? _supervisor;

        /// <param name="supervisor">Null in standalone mode.</param>
        public WorkersViewModel(IExecutionService execution, RemoteWorkersService? supervisor)
        {
            _execution = execution;
            _supervisor = supervisor;
        }

        [RelayCommand]
        public async Task RefreshAsync()
        {
            List<ExecutionSummary> localRunning = [.. _execution.GetRunningLocally()
                .Select(x => new ExecutionSummary(x.Id, x.TaskId, x.NodeName, x.CreatedAt))];
            List<WorkerItem> workers = [WorkerItem.Local(localRunning)];

            if (_supervisor == null)
            {
                Workers = workers;
                return;
            }

            try
            {
                WorkersOverview overview = await _supervisor.GetOverviewAsync();
                workers.AddRange(overview.Workers.Select(WorkerItem.Remote));
                Waiting = overview.Waiting;
                SupervisorError = null;
            }
            catch (HttpRequestException exception)
            {
                // The application still runs its own tasks : it is listed all the same.
                Waiting = [];
                SupervisorError = $"The supervisor can't be reached : {exception.Message}";
            }

            Workers = workers;
        }
    }
}
