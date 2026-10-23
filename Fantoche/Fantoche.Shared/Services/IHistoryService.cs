using Fantoche.Shared.Base;
using Fantoche.Shared.Data.Execution;

namespace Fantoche.Shared.Services;

public interface IHistoryService
{
    public Task<Paginated<TaskInstance>> GetByScopedAsync(Guid elementId, PaginationOptions options = default);

    /// <summary>
    /// The instances run by the workflow instance [instanceId], oldest first : the nodes of its
    /// graph, a nested workflow holding its own in turn. Empty for the instance of a simple task.
    /// </summary>
    public Task<IReadOnlyList<TaskInstance>> GetChildrenAsync(Guid instanceId);

    /// <summary>
    /// Every instance run under [instanceId], oldest first : the ones of its nested workflows too,
    /// what a resumed run is walked again from.
    /// </summary>
    public Task<IReadOnlyList<TaskInstance>> GetDescendantsAsync(Guid instanceId);

    /// <summary>
    /// Mark the runs of [applicationId] (null for the ones the supervisor dispatches) started before
    /// [startedBefore] and left unfinished as interrupted, with the instances they were running.
    /// Called as the application or the supervisor starts : nothing runs what they started before.
    /// Returns the runs.
    /// </summary>
    public Task<IReadOnlyList<TaskInstance>> InterruptRunsAsync(string? applicationId, DateTime startedBefore);

    /// <summary>
    /// The instance [instanceId] as it was last saved, null when nothing ran under that id.
    /// </summary>
    public Task<TaskInstance?> GetAsync(Guid instanceId);

    /// <summary>
    /// Store [instance] : added the first time it is seen, updated afterwards. Raises
    /// <see cref="InstanceAdded"/> or <see cref="InstanceUpdated"/>.
    /// </summary>
    public Task SaveAsync(TaskInstance instance);

    /// <summary>
    /// Raised when a new task instance is added to the history.
    /// </summary>
    public event Action<TaskInstance>? InstanceAdded;

    /// <summary>
    /// Raised when an existing task instance changed (e.g. its state).
    /// </summary>
    public event Action<TaskInstance>? InstanceUpdated;
}
