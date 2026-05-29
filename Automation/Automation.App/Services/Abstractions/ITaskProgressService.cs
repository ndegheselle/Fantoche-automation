using Automation.Shared.Data.Execution;

namespace Automation.App.Services.Abstractions
{
    /// <summary>
    /// Realtime task-instance progress (replaces the SignalR TaskProgressHubClient). The old
    /// TaskInstanceNotification / TaskInstanceState message types were removed; this contract uses
    /// current domain types. The concrete message/transport is re-established in the data rework.
    /// </summary>
    public interface ITaskProgressService
    {
        /// <summary>Raised when a task instance changes state.</summary>
        event Action<Guid, EnumTaskState>? StateChanged;

        /// <summary>Raised when a task instance is updated (progress/notification).</summary>
        event Action<TaskInstance>? InstanceUpdated;

        Task StartAsync(CancellationToken cancellationToken = default);

        Task StopAsync();
    }
}
