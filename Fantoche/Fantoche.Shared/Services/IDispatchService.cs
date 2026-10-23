using Fantoche.Shared.Remote;

namespace Fantoche.Shared.Services;

/// <summary>
/// The workers of the supervisor, what they run and what waits for one. Only held in the memory of
/// the supervisor : it is pushed as it changes rather than stored.
/// </summary>
public interface IDispatchService
{
    public Task<DispatchStatus> GetStatusAsync();

    /// <summary>
    /// Raised with the new status whenever a worker comes or goes, or an execution is queued,
    /// handed to a worker or over.
    /// </summary>
    public event Action<DispatchStatus>? StatusChanged;
}
