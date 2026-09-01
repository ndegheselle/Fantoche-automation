using Automation.Shared.Base;
using Automation.Shared.Data.Execution;

namespace Automation.Shared.Services;

public interface IHistoryService
{
    public Task<Paginated<TaskInstance>> GetByScopedAsync(Guid elementId, PaginationOptions options = default);

    /// <summary>
    /// The instances run by the workflow instance [instanceId], oldest first : the nodes of its
    /// graph, a nested workflow holding its own in turn. Empty for the instance of a simple task.
    /// </summary>
    public Task<IReadOnlyList<TaskInstance>> GetChildrenAsync(Guid instanceId);

    /// <summary>
    /// Raised when a new task instance is added to the history.
    /// </summary>
    public event Action<TaskInstance>? InstanceAdded;

    /// <summary>
    /// Raised when an existing task instance changed (e.g. its state).
    /// </summary>
    public event Action<TaskInstance>? InstanceUpdated;
}
