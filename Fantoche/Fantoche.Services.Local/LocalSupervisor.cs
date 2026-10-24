using Fantoche.Plugins.Shared;
using Fantoche.Services.Local.Database;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Remote;
using Fantoche.Shared.Services;
using Fantoche.Worker;
using Fantoche.Worker.Packages;
using Fantoche.Worker.Storage;

namespace Fantoche.Services.Local;

/// <summary>
/// The supervisor itself : the database, the packages, the dispatch of the executions to the
/// workers and the schedules starting them. Run by the application in standalone mode and by the
/// supervisor API otherwise, which only carries its calls over HTTP and its hubs.
/// <para>
/// Its data folder holds its database ("fantoche.db"), the packages ("nuget"), the assemblies
/// extracted from them to check what they hold ("cache"), the lock files of its sessions ("sessions").
/// </para>
/// <para>
/// The executions are kept a month, their history and their storage (see <see cref="CleanUpAsync"/>).
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

        public Task Execute(TaskInstance instance)
            => _dispatcher.SubmitAsync(_connectionId, this, FantocheJson.Copy(instance));

        public Task Cancel(Guid executionId) => _dispatcher.CancelAsync(_connectionId, executionId);

        public Task Notified(Guid executionId, TaskNotification notification) => _caller.Notified(executionId, notification);

        public Task Completed(TaskInstance instance) => _caller.Completed(FantocheJson.Copy(instance));
    }

    /// <summary>
    /// A worker of this process, its messages copied the same way (see <see cref="CallerLink"/>).
    /// Removing it is losing its connection : the supervisor suspends what it was running, and what
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
            => _isRemoved ? Task.CompletedTask : _dispatcher.ReportStateAsync(_connectionId, executionId, FantocheJson.Copy(instance));

        public Task ReportNotification(Guid executionId, TaskNotification notification)
            => _isRemoved ? Task.CompletedTask : _dispatcher.ReportNotificationAsync(executionId, notification);

        public Task Complete(TaskInstance instance)
            => _isRemoved ? Task.CompletedTask : _dispatcher.CompleteAsync(_connectionId, FantocheJson.Copy(instance));

        public Task Execute(RemoteExecution execution) => _session.Execute(FantocheJson.Copy(execution));

        public Task Cancel(Guid executionId) => _session.Cancel(executionId);
    }

    private readonly DatabaseFactory _database;
    /// <summary>The storage the workers share, null when the supervisor doesn't reach it.</summary>
    private readonly ExecutionStorages? _storages;

    public LocalScopedService Scoped { get; }
    public LocalHistoryService History { get; }
    public LocalPackagesService Packages { get; }
    public Dispatcher Dispatcher { get; }
    public Scheduler Scheduler { get; }
    /// <summary>
    /// The supervisors sharing its database : copies of the application in standalone mode, whose
    /// own runs are marked with the same session.
    /// </summary>
    public RunnerSessions Sessions { get; }

    IScopedService ISupervisor.Scoped => Scoped;
    IHistoryService ISupervisor.History => History;
    IPackagesService ISupervisor.Packages => Packages;
    IPackageSource ISupervisor.PackageFiles => Packages;
    IDispatchService ISupervisor.Dispatch => Dispatcher;

    /// <param name="storageFolder">
    /// The storage the workers share, cleaned along with the history. Null when the supervisor
    /// doesn't reach it.
    /// </param>
    public LocalSupervisor(string dataFolder, string? storageFolder = null)
    {
        _storages = storageFolder == null ? null : new ExecutionStorages(storageFolder);
        // The factory puts the schema and the starting content in place as it is built.
        _database = new DatabaseFactory(Path.Combine(dataFolder, "fantoche.db"));
        Scoped = new LocalScopedService(_database);
        History = new LocalHistoryService(_database);
        Packages = new LocalPackagesService(Path.Combine(dataFolder, "nuget"), Path.Combine(dataFolder, "cache"));
        Sessions = new RunnerSessions(Path.Combine(dataFolder, "sessions"));
        Dispatcher = new Dispatcher(Scoped, History, Sessions.CurrentId);
        Scheduler = new Scheduler(_database, Dispatcher);

        // Nothing a supervisor dispatched outlives it : what the history holds as running for a
        // supervisor that is gone was cut short. Run apart so a caller holding a synchronization
        // context doesn't wait on itself.
        Task.Run(() => Sessions.SuspendLeftRunsAsync(History, applicationId: null)).GetAwaiter().GetResult();
    }

    /// <summary>Take the cost of opening the database off the first read.</summary>
    public void WarmUpDatabase() => DatabaseFactory.WarmUpDatabase(_database);

    /// <summary>
    /// Remove the executions started more than a month before [now] : their history and, when the
    /// supervisor reaches it, their storage.
    /// </summary>
    public async Task CleanUpAsync(DateTime now)
    {
        DateTime cutoff = now.AddMonths(-1);
        // The folders first : once the runs are gone, nothing tells which folders were theirs.
        if (_storages != null)
        {
            foreach (TaskInstance run in await History.GetRunsStartedBeforeAsync(cutoff))
                _storages.Remove(run);
        }
        await History.DeleteStartedBeforeAsync(cutoff);
        await Scheduler.ForgetBeforeAsync(cutoff);
    }

    /// <summary>Clean up now, then once a day until [cancellation].</summary>
    public async Task RunCleanupAsync(CancellationToken cancellation)
    {
        using PeriodicTimer timer = new(TimeSpan.FromDays(1));
        try
        {
            do
            {
                try
                {
                    await CleanUpAsync(DateTime.UtcNow);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    // Nothing is lost by keeping an execution one more day : the next tick retries.
                }
            }
            while (await timer.WaitForNextTickAsync(cancellation));
        }
        catch (OperationCanceledException)
        {
            // Stopping.
        }
    }

    public ICallerHub ConnectCaller(ICallerClient caller) => new CallerLink(Dispatcher, caller);

    public async Task<IAsyncDisposable> AddWorkerAsync(LocalWorker worker, string workerId, int capacity, CancellationToken cancellation = default)
    {
        WorkerLink link = new(Dispatcher, worker);
        await link.Register(workerId, capacity);
        return link;
    }
}
