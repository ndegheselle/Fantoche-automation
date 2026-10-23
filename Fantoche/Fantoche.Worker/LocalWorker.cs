using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Scoped;
using Fantoche.Shared.Remote;
using Fantoche.Worker.Executor;
using Fantoche.Worker.Packages;
using Fantoche.Worker.Storage;

namespace Fantoche.Worker;

/// <summary>
/// What runs the tasks and workflows, in the application as in the worker service : the packages
/// downloaded from the supervisor, a storage for each execution (see <see cref="ExecutionStorages"/>)
/// and the tasks set to run on a worker sent through the supervisor.
/// <para>
/// It runs the executions started in its process (as an <see cref="ITaskExecutor"/>, see
/// <see cref="ExecutionService"/>) and, once added to the workers of the supervisor (see
/// <see cref="ISupervisor.AddWorkerAsync"/>), the ones the supervisor dispatches to it.
/// </para>
/// </summary>
public sealed class LocalWorker : ITaskExecutor, IDisposable
{
    private readonly TaskLoaders _tasks;
    private readonly ExecutionStorages _storages;
    private readonly IStorageTransit _transit;
    private readonly RemoteTaskExecutor _remote;

    /// <param name="storageFolder">Where the storage of its executions is kept.</param>
    public LocalWorker(ISupervisor supervisor, IPackageManagement packages, string storageFolder)
    {
        _tasks = new TaskLoaders(packages);
        _storages = new ExecutionStorages(storageFolder);
        _transit = supervisor.Transit;
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

    /// <summary>The space the storage of its executions takes, for each task or workflow.</summary>
    public List<StorageUsage> GetStorageUsage() => _storages.GetUsage();

    /// <summary>
    /// An execution started in this process : the nodes set to run on a worker are sent to the
    /// supervisor.
    /// </summary>
    public async Task<TaskInstance> ExecuteAsync(
        BaseAutomationTask task,
        TaskInstance instance,
        TaskInstancesProgress? progress = null,
        CancellationToken? cancellation = null)
    {
        LocalStorage storage = _storages.Open(task.Id, instance);
        try
        {
            MixedTaskExecutor executor = new(_tasks, storage, _remote, _transit);
            return await executor.ExecuteAsync(task, instance, progress, cancellation);
        }
        finally
        {
            _storages.Release(task.Id, instance, task.Settings.StorageRetention);
        }
    }

    /// <summary>
    /// An execution the supervisor dispatched : the storage it is handed is taken from the
    /// transit, and the storage its output points at sent back.
    /// </summary>
    public async Task<TaskInstance> ExecuteDispatchedAsync(
        RemoteExecution execution,
        TaskInstancesProgress progress,
        CancellationToken cancellation)
    {
        var (task, instance) = execution.Unpack();
        LocalStorage storage = _storages.Open(task.Id, instance);
        try
        {
            await StorageTransfer.ReceiveAsync(_transit, instance.Id, storage);

            // Once on a worker an execution stays on it : its nodes all run here, whatever their setting.
            LocalTaskExecutor executor = new(_tasks, storage);
            TaskInstance result = await executor.ExecuteAsync(task, instance, progress, cancellation);

            if (result.State == EnumTaskState.Completed)
                await StorageTransfer.SendAsync(storage, result.Output, _transit, instance.Id);
            return result;
        }
        finally
        {
            _storages.Release(task.Id, instance, task.Settings.StorageRetention);
        }
    }
}
