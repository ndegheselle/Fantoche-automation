using Automation.Shared.Data.Execution;

namespace Automation.Shared.Services;

public interface IWorkersService
{
    /// <summary>
    /// Get all the currently registered workers.
    /// </summary>
    public Task<IEnumerable<WorkerInfos>> GetWorkersAsync();

    /// <summary>
    /// Raised when a worker is registered or its state/load changed.
    /// </summary>
    public event Action<WorkerInfos>? WorkerUpdated;

    /// <summary>
    /// Raised when a worker is removed (unregistered or considered dead).
    /// </summary>
    public event Action<string>? WorkerRemoved;
}
