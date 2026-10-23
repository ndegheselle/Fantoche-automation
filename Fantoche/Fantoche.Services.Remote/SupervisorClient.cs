using Fantoche.Shared.Remote;
using Fantoche.Shared.Services;
using Fantoche.Worker;
using Fantoche.Worker.Packages;
using Fantoche.Worker.Storage;

namespace Fantoche.Services.Remote;

/// <summary>
/// A supervisor reached through its API : its data over HTTP, the executions and the changes of its
/// history over its hubs. It behaves as the supervisor of a process does, the API only carrying the
/// calls.
/// </summary>
public sealed class SupervisorClient : ISupervisor, IAsyncDisposable
{
    private readonly Uri _address;
    private readonly HttpClient _http;
    private readonly List<RemoteCallerConnection> _callers = [];

    public RemoteScopedService Scoped { get; }
    /// <summary>Its changes are only heard once <see cref="RemoteHistoryService.StartAsync"/> connected.</summary>
    public RemoteHistoryService History { get; }
    public RemotePackagesService Packages { get; }
    public RemotePackageSource PackageFiles { get; }
    public RemoteStorageTransit Transit { get; }

    IScopedService ISupervisor.Scoped => Scoped;
    IHistoryService ISupervisor.History => History;
    IPackagesService ISupervisor.Packages => Packages;
    IPackageSource ISupervisor.PackageFiles => PackageFiles;
    IStorageTransit ISupervisor.Transit => Transit;

    /// <param name="address">The base of the addresses of the supervisor, ending with a "/".</param>
    public SupervisorClient(Uri address)
    {
        _address = address;
        _http = new HttpClient { BaseAddress = address };
        Scoped = new RemoteScopedService(_http);
        History = new RemoteHistoryService(_http);
        Packages = new RemotePackagesService(_http);
        PackageFiles = new RemotePackageSource(_http);
        Transit = new RemoteStorageTransit(_http);
    }

    public async ValueTask DisposeAsync()
    {
        List<RemoteCallerConnection> callers;
        lock (_callers)
            callers = [.. _callers];

        foreach (RemoteCallerConnection caller in callers)
            await caller.DisposeAsync();
        await History.DisposeAsync();
        _http.Dispose();
    }

    public ICallerHub ConnectCaller(ICallerClient caller)
    {
        RemoteCallerConnection connection = new(_address, caller);
        lock (_callers)
            _callers.Add(connection);
        return connection;
    }

    public async Task<IAsyncDisposable> AddWorkerAsync(LocalWorker worker, string workerId, int capacity, CancellationToken cancellation = default)
    {
        RemoteWorkerConnection connection = new(_address, worker, workerId, capacity);
        try
        {
            await connection.StartAsync(cancellation);
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
        return connection;
    }
}
