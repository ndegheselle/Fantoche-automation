using Fantoche.Plugins.Shared;
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
    /// <param name="capacity">How many runs it runs at once : the tasks asking the user don't count.</param>
    /// <param name="roles">What it is dispatched : runs, the tasks asking the user (an application), or both.</param>
    Task<IAsyncDisposable> AddWorkerAsync(LocalWorker worker, string workerId, int capacity, EnumWorkerRoles roles, CancellationToken cancellation = default);

    /// <summary>
    /// The storage of the run [runId], held by the workers of the supervisor : what a task asking the
    /// user reads and writes from the application it runs in.
    /// </summary>
    Task<IStorage> OpenStorageAsync(Guid runId);
}
