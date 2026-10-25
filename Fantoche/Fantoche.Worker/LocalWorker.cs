using Fantoche.Plugins.Shared;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Scoped;
using Fantoche.Shared.Remote;
using Fantoche.Worker.Executor;
using Fantoche.Worker.Packages;
using Fantoche.Worker.Storage;
using Fantoche.Worker.UserInterfaces;

namespace Fantoche.Worker;

/// <summary>
/// What runs the tasks and workflows, in the application as in the worker service : the packages
/// downloaded from the supervisor and a storage for each execution (see <see cref="ExecutionStorages"/>).
/// <para>
/// It runs the executions started in its process (as an <see cref="ITaskExecutor"/>, see
/// <see cref="ExecutionService"/>) and, once added to the workers of the supervisor (see
/// <see cref="ISupervisor.AddWorkerAsync"/>), the ones the supervisor dispatches to it.
/// </para>
/// <para>
/// Once an execution started somewhere, everything of it stays there : a workflow runs all its
/// nodes where it runs, and its tables and files never leave the storage of its run. A run resumed
/// after a crash carries on wherever that storage is reached from (see <see cref="ExecutionStorages"/>).
/// The tasks asking the user are the exception : they run in an application (see <see cref="UserTasks"/>),
/// reaching the storage of their run through the supervisor when it is held elsewhere.
/// </para>
/// </summary>
public sealed class LocalWorker : ITaskExecutor, IDisposable
{
    private readonly TaskLoaders _tasks;
    private readonly ExecutionStorages _storages;
    private readonly UserTasks _users;
    private readonly Func<Guid, Task<IStorage>>? _runStorages;

    /// <summary>Where the storage of its executions is kept.</summary>
    public string StorageFolder { get; }

    /// <param name="storageFolder">Where the storage of its executions is kept.</param>
    /// <param name="users">Where the tasks asking the user run, none by default.</param>
    /// <param name="runStorages">
    /// The storage of a run held elsewhere, for the tasks asking the user handed over by the worker
    /// running it (see <see cref="ISupervisor.OpenStorageAsync"/>).
    /// </param>
    public LocalWorker(IPackageManagement packages, string storageFolder, UserTasks? users = null, Func<Guid, Task<IStorage>>? runStorages = null)
    {
        _tasks = new TaskLoaders(packages);
        _storages = new ExecutionStorages(storageFolder);
        _users = users ?? UserTasks.None;
        _runStorages = runStorages;
        StorageFolder = storageFolder;
    }

    /// <summary>
    /// A worker keeping its data in [dataFolder] : the packages extracted in "cache" and the
    /// storage in "storage", unless [storageFolder] is shared with other workers. With [pages], it is
    /// the one of an application and runs the tasks asking the user itself ; otherwise it hands them
    /// to an application through [supervisor].
    /// </summary>
    public static LocalWorker InFolder(ISupervisor supervisor, string dataFolder, string? storageFolder = null, IPageHost? pages = null)
    {
        PackageCache packages = new(supervisor.PackageFiles, Path.Combine(dataFolder, "cache"));
        UserTasks users = pages == null
            ? UserTasks.HandedTo(new RemoteTaskExecutor(supervisor))
            : UserTasks.ShownBy(pages);
        return new LocalWorker(packages, storageFolder ?? Path.Combine(dataFolder, "storage"), users, supervisor.OpenStorageAsync);
    }

    public void Dispose() => _tasks.Dispose();

    /// <summary>An execution started in this process.</summary>
    public async Task<TaskInstance> ExecuteAsync(
        BaseAutomationTask task,
        TaskInstance instance,
        TaskInstancesProgress? progress = null,
        CancellationToken? cancellation = null)
    {
        LocalStorage storage = _storages.Open(task.Id, instance);
        LocalTaskExecutor executor = new(_tasks, storage, _users, instance.Id);
        return await executor.ExecuteAsync(task, instance, progress, cancellation);
    }

    /// <summary>
    /// An execution the supervisor dispatched : a whole run, or a task asking the user handed over by
    /// the worker running its run, whose storage it uses.
    /// </summary>
    public async Task<TaskInstance> ExecuteDispatchedAsync(
        RemoteExecution execution,
        TaskInstancesProgress progress,
        CancellationToken cancellation)
    {
        var (task, instance) = execution.Unpack();
        if (execution.RunId is not Guid runId)
            return await ExecuteAsync(task, instance, progress, cancellation);

        Func<Guid, Task<IStorage>> runStorages = _runStorages
            ?? throw new ExecutionException($"'{instance.NodeName}' belongs to a run held elsewhere, whose storage this worker can't reach.");
        IStorage storage = await runStorages(runId);
        LocalTaskExecutor executor = new(_tasks, storage, _users, runId);
        return await executor.ExecuteAsync(task, instance, progress, cancellation);
    }
}
