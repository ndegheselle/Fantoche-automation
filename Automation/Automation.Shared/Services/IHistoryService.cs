using Automation.Shared.Base;
using Automation.Shared.Data.Execution;

namespace Automation.Shared.Services;

public interface IHistoryService
{
    /// <summary>
    /// Get a page of the executed task instances, most recent first.
    /// </summary>
    public Task<Paginated<TaskInstance>> SearchAsync(PaginationOptions options = default);

    /// <summary>
    /// Raised when a new task instance is added to the history.
    /// </summary>
    public event Action<TaskInstance>? InstanceAdded;

    /// <summary>
    /// Raised when an existing task instance changed (e.g. its state).
    /// </summary>
    public event Action<TaskInstance>? InstanceUpdated;
}
