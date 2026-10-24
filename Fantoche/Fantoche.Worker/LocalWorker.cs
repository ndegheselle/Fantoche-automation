using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Scoped;
using Fantoche.Shared.Remote;
using Fantoche.Worker.Executor;
using Fantoche.Worker.Packages;
using Fantoche.Worker.Storage;

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
/// </para>
/// </summary>
public sealed class LocalWorker : ITaskExecutor, IDisposable
{
    private readonly TaskLoaders _tasks;
    private readonly ExecutionStorages _storages;

    /// <summary>Where the storage of its executions is kept.</summary>
    public string StorageFolder { get; }

    /// <param name="storageFolder">Where the storage of its executions is kept.</param>
    public LocalWorker(IPackageManagement packages, string storageFolder)
    {
        _tasks = new TaskLoaders(packages);
        _storages = new ExecutionStorages(storageFolder);
        StorageFolder = storageFolder;
    }

    /// <summary>
    /// A worker keeping its data in [dataFolder] : the packages extracted in "cache" and the
    /// storage in "storage", unless [storageFolder] is shared with other workers.
    /// </summary>
    public static LocalWorker InFolder(ISupervisor supervisor, string dataFolder, string? storageFolder = null)
    {
        PackageCache packages = new(supervisor.PackageFiles, Path.Combine(dataFolder, "cache"));
        return new LocalWorker(packages, storageFolder ?? Path.Combine(dataFolder, "storage"));
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
        LocalTaskExecutor executor = new(_tasks, storage);
        return await executor.ExecuteAsync(task, instance, progress, cancellation);
    }

    /// <summary>
    /// The folder of the page of the user interface task [task], extracted from its package on this
    /// machine : the page is shown where the user is.
    /// </summary>
    public Task<string> GetPageFolderAsync(AutomationTask task)
    {
        if (task.Target is not PackageClassTarget target)
            throw new InvalidOperationException($"The task '{task.Metadata.Name}' isn't in a package.");
        return _tasks.GetPageFolderAsync(target);
    }

    /// <summary>
    /// The storage of [run] on this machine, where its nodes keep their tables and files. Empty for
    /// a run dispatched to the remote workers : its storage is theirs.
    /// </summary>
    public LocalStorage OpenStorage(TaskInstance run) => _storages.Open(run.TaskId, run);

    /// <summary>An execution the supervisor dispatched.</summary>
    public Task<TaskInstance> ExecuteDispatchedAsync(
        RemoteExecution execution,
        TaskInstancesProgress progress,
        CancellationToken cancellation)
    {
        var (task, instance) = execution.Unpack();
        return ExecuteAsync(task, instance, progress, cancellation);
    }
}
