using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Remote;
using Newtonsoft.Json.Linq;

namespace Fantoche.Shared.Services;

/// <summary>
/// The workers of the supervisor, what they run and what waits for one. Only held in the memory of
/// the supervisor : it is pushed as it changes rather than stored.
/// </summary>
public interface IDispatchService
{
    public Task<DispatchStatus> GetStatusAsync();

    /// <summary>
    /// Start [taskId], named [name] in the history, with [parameters] on whichever worker has room.
    /// Nobody waits on it : it is followed through the history, where one that can't be loaded is
    /// stored as failed.
    /// </summary>
    public Task<TaskInstance> StartAsync(Guid taskId, string name, JToken? parameters);

    /// <summary>
    /// Resume the suspended run [runId] the supervisor dispatched, on whichever worker has
    /// room : the nodes it was running run again, the rest of it carrying on from what the history holds.
    /// </summary>
    /// <exception cref="ExecutionException">The run can't be resumed, see <see cref="Data.Execution.RunResumption"/>.</exception>
    public Task ResumeAsync(Guid runId);

    /// <summary>
    /// Raised with the new status whenever a worker comes or goes, or an execution is queued,
    /// handed to a worker or over.
    /// </summary>
    public event Action<DispatchStatus>? StatusChanged;
}
