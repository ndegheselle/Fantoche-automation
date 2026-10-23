using Fantoche.Plugins.Shared;
using Fantoche.Services.Local.Database;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Remote;
using Fantoche.Shared.Services;
using Fantoche.Worker;
using Fantoche.Worker.Packages;
using Fantoche.Worker.Storage;
using Newtonsoft.Json.Linq;

namespace Fantoche.Services.Local;

/// <summary>
/// The supervisor itself : the database, the packages, the storage in transit and the dispatch of
/// the executions to the workers. Run by the application in standalone mode and by the supervisor
/// API otherwise, which only carries its calls over HTTP and its hubs.
/// <para>
/// Its data folder holds its database ("fantoche.db"), the packages ("nuget"), the assemblies
/// extracted from them to check what they hold ("cache") and the storage in transit ("transit").
/// </para>
/// </summary>
public sealed class LocalSupervisor : ISupervisor
{
    /// <summary>
    /// A caller of this process. Its messages are copied as they would be over the wire : the
    /// caller and the worker never share an instance, as with a remote supervisor.
    /// </summary>
    private sealed class CallerLink : ICallerHub, ICallerClient
    {
        private readonly string _connectionId = Guid.NewGuid().ToString("N");
        private readonly Dispatcher _dispatcher;
        private readonly ICallerClient _caller;

        public CallerLink(Dispatcher dispatcher, ICallerClient caller)
        {
            _dispatcher = dispatcher;
            _caller = caller;
        }

        public Task Execute(TaskInstance instance, JToken? globalContext)
            => _dispatcher.SubmitAsync(_connectionId, this, FantocheJson.Copy(instance), globalContext?.DeepClone());

        public Task Cancel(Guid executionId) => _dispatcher.CancelAsync(_connectionId, executionId);

        public Task Notified(Guid executionId, TaskNotification notification) => _caller.Notified(executionId, notification);

        public Task Completed(TaskInstance instance) => _caller.Completed(FantocheJson.Copy(instance));
    }

    /// <summary>
    /// A worker of this process, its messages copied the same way (see <see cref="CallerLink"/>).
    /// Removing it is losing its connection : the supervisor fails what it was running, and what
    /// it still sends about it is lost.
    /// </summary>
    private sealed class WorkerLink : IWorkerHub, IWorkerClient, IAsyncDisposable
    {
        private readonly string _connectionId = Guid.NewGuid().ToString("N");
        private readonly Dispatcher _dispatcher;
        private readonly WorkerSession _session;
        private volatile bool _isRemoved;

        public WorkerLink(Dispatcher dispatcher, LocalWorker worker)
        {
            _dispatcher = dispatcher;
            _session = new WorkerSession(worker, this);
        }

        public async ValueTask DisposeAsync()
        {
            _isRemoved = true;
            await _dispatcher.RemoveWorkerAsync(_connectionId);
            _session.CancelAll();
        }

        public Task Register(string workerId, int capacity) => _dispatcher.AddWorkerAsync(_connectionId, this, workerId, capacity);

        public Task ReportState(Guid executionId, TaskInstance instance)
            => _isRemoved ? Task.CompletedTask : _dispatcher.ReportStateAsync(FantocheJson.Copy(instance));

        public Task ReportNotification(Guid executionId, TaskNotification notification)
            => _isRemoved ? Task.CompletedTask : _dispatcher.ReportNotificationAsync(executionId, notification);

        public Task Complete(TaskInstance instance)
            => _isRemoved ? Task.CompletedTask : _dispatcher.CompleteAsync(_connectionId, FantocheJson.Copy(instance));

        public Task Execute(RemoteExecution execution) => _session.Execute(FantocheJson.Copy(execution));

        public Task Cancel(Guid executionId) => _session.Cancel(executionId);
    }

    private readonly DatabaseFactory _database;

    public LocalScopedService Scoped { get; }
    public LocalHistoryService History { get; }
    public LocalPackagesService Packages { get; }
    public LocalStorageTransit Transit { get; }
    public Dispatcher Dispatcher { get; }

    IScopedService ISupervisor.Scoped => Scoped;
    IHistoryService ISupervisor.History => History;
    IPackagesService ISupervisor.Packages => Packages;
    IPackageSource ISupervisor.PackageFiles => Packages;
    IStorageTransit ISupervisor.Transit => Transit;

    public LocalSupervisor(string dataFolder)
    {
        // The factory puts the schema and the starting content in place as it is built.
        _database = new DatabaseFactory(Path.Combine(dataFolder, "fantoche.db"));
        Scoped = new LocalScopedService(_database);
        History = new LocalHistoryService(_database);
        Packages = new LocalPackagesService(Path.Combine(dataFolder, "nuget"), Path.Combine(dataFolder, "cache"));
        Transit = new LocalStorageTransit(Path.Combine(dataFolder, "transit"));
        Transit.Reset();
        Dispatcher = new Dispatcher(Scoped, History, Transit);
    }

    /// <summary>Take the cost of opening the database off the first read.</summary>
    public void WarmUpDatabase() => DatabaseFactory.WarmUpDatabase(_database);

    public ICallerHub ConnectCaller(ICallerClient caller) => new CallerLink(Dispatcher, caller);

    public async Task<IAsyncDisposable> AddWorkerAsync(LocalWorker worker, string workerId, int capacity, CancellationToken cancellation = default)
    {
        WorkerLink link = new(Dispatcher, worker);
        await link.Register(workerId, capacity);
        return link;
    }
}
