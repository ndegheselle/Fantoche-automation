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
/// nodes where it runs, whatever their own setting, and its tables and files never leave the
/// storage of its run.
/// </para>
/// </summary>
public sealed class LocalWorker : ITaskExecutor, IDisposable
{
    private readonly TaskLoaders _tasks;
    private readonly ExecutionStorages _storages;
    private readonly RemoteTaskExecutor _remote;

    /// <param name="storageFolder">Where the storage of its executions is kept.</param>
    public LocalWorker(ISupervisor supervisor, IPackageManagement packages, string storageFolder)
    {
        _tasks = new TaskLoaders(packages);
        _storages = new ExecutionStorages(storageFolder);
        _remote = new RemoteTaskExecutor(supervisor);
    }

    /// <summary>
    /// A worker keeping its data in [dataFolder] : the packages extracted in "cache" and the
    /// storage in "storage".
    /// </summary>
    public static LocalWorker InFolder(ISupervisor supervisor, string dataFolder)
    {
        PackageCache packages = new(supervisor.PackageFiles, Path.Combine(dataFolder, "cache"));
        return new LocalWorker(supervisor, packages, Path.Combine(dataFolder, "storage"));
    }

    public void Dispose() => _tasks.Dispose();

    /// <summary>
    /// An execution started in this process : sent whole to the supervisor when [task] is set to
    /// run on a worker (see <see cref="TaskSettings.Execution"/>), run here otherwise.
    /// </summary>
    public Task<TaskInstance> ExecuteAsync(
        BaseAutomationTask task,
        TaskInstance instance,
        TaskInstancesProgress? progress = null,
        CancellationToken? cancellation = null)
    {
        if (task.Settings.Execution == EnumExecutionMode.Remote)
            return _remote.ExecuteAsync(task, instance, progress, cancellation);

        return RunHereAsync(task, instance, progress, cancellation);
    }

    /// <summary>An execution the supervisor dispatched.</summary>
    public Task<TaskInstance> ExecuteDispatchedAsync(
        RemoteExecution execution,
        TaskInstancesProgress progress,
        CancellationToken cancellation)
    {
        var (task, instance) = execution.Unpack();
        return RunHereAsync(task, instance, progress, cancellation);
    }

    private async Task<TaskInstance> RunHereAsync(
        BaseAutomationTask task,
        TaskInstance instance,
        TaskInstancesProgress? progress,
        CancellationToken? cancellation)
    {
        LocalStorage storage = _storages.Open(task.Id, instance);
        try
        {
            LocalTaskExecutor executor = new(_tasks, storage);
            return await executor.ExecuteAsync(task, instance, progress, cancellation);
        }
        finally
        {
            _storages.Release(task.Id, instance, task.Settings.StorageRetention);
        }
    }
}
