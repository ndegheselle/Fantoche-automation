using Fantoche.Shared.Remote;
using Fantoche.Shared.Services;
using Fantoche.Worker.Packages;

namespace Fantoche.Worker;

/// <summary>
/// The supervisor as an application or a worker reaches it : run in the same process (the local
/// supervisor of the standalone mode and of the supervisor API) or through its API (the client of
/// a remote supervisor). Both behave the same, the API only carries the calls.
/// </summary>
public interface ISupervisor
{
    IScopedService Scoped { get; }

    IHistoryService History { get; }

    IPackagesService Packages { get; }

    /// <summary>The files of the packages, which a worker extracts to load the tasks from.</summary>
    IPackageSource PackageFiles { get; }

    /// <summary>
    /// What [caller] sends executions to the workers with, hearing back about them as they run.
    /// </summary>
    ICallerHub ConnectCaller(ICallerClient caller);

    IDispatchService Dispatch { get; }

    /// <summary>
    /// Add [worker] to the ones the executions are dispatched to, until the returned registration
    /// is disposed. Waits for the supervisor to be reachable.
    /// </summary>
    /// <param name="capacity">How many executions it runs at once.</param>
    Task<IAsyncDisposable> AddWorkerAsync(LocalWorker worker, string workerId, int capacity, CancellationToken cancellation = default);
}
