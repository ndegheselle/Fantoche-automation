using Automation.Shared.Base;
using Automation.Shared.Data.Execution;

namespace Automation.Shared.Services;

public interface IHistoryService
{
    /// <summary>
    /// Get a page of the executed task instances, most recent first.
    /// When [taskIds] is provided, only instances whose <see cref="TaskInstance.TaskId"/> is in the
    /// set are returned (used to scope the history to a given element).
    /// </summary>
    public Task<Paginated<TaskInstance>> SearchAsync(
        PaginationOptions options = default,
        IReadOnlyCollection<Guid>? taskIds = null);

    /// <summary>
    /// Raised when a new task instance is added to the history.
    /// </summary>
    public event Action<TaskInstance>? InstanceAdded;

    /// <summary>
    /// Raised when an existing task instance changed (e.g. its state).
    /// </summary>
    public event Action<TaskInstance>? InstanceUpdated;
}
