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
    /// The runs of [applicationId] (null for the ones the supervisor dispatches) that aren't over :
    /// running, or left behind by a process that is gone (see <see cref="TaskInstance.SessionId"/>).
    /// </summary>
    public Task<IReadOnlyList<TaskInstance>> GetUnfinishedRunsAsync(string? applicationId);

    /// <summary>
    /// Mark the run [runId] as interrupted, with the instances it was running : nothing runs it
    /// anymore. A join waiting for its branches stays as it is, that is what a resume carries on
    /// from. Nothing happens to a run that is over.
    /// </summary>
    public Task InterruptRunAsync(Guid runId);

    /// <summary>
    /// Take the interrupted or paused run [runId] for the session [sessionId] about to resume it :
    /// false when it isn't interrupted or paused anymore, another process having taken it first.
    /// </summary>
    public Task<bool> ClaimRunAsync(Guid runId, string sessionId);

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
