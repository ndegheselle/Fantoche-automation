using Automation.App.Services.Abstractions;
using Automation.Shared.Data.Execution;

namespace Automation.App.Services.Stubs
{
    /// <summary>
    /// Benign placeholder pending the worker+SQLite rework: never raises events, start/stop are
    /// no-ops (realtime is a background concern, so this does not throw). See NotImplementedScopesService.
    /// </summary>
    public class NotImplementedTaskProgressService : ITaskProgressService
    {
        public event Action<Guid, EnumTaskState>? StateChanged;
        public event Action<TaskInstance>? InstanceUpdated;

        public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task StopAsync() => Task.CompletedTask;

        // Referenced to avoid "event never used" warnings; intentionally never invoked in the stub.
        private void Suppress()
        {
            StateChanged?.Invoke(Guid.Empty, default);
            InstanceUpdated?.Invoke(null!);
        }
    }
}
