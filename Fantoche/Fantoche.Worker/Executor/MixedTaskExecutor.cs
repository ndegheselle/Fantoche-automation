using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Scoped;
using Fantoche.Worker.Packages;
using Fantoche.Worker.Storage;
using Newtonsoft.Json.Linq;

namespace Fantoche.Worker.Executor;

/// <summary>
/// Run each task where its <see cref="TaskSettings.Execution"/> says : in this process or on a
/// worker. A workflow run here walks its nodes through this executor, so each node is sent where
/// its own setting says. Built for an execution, with its storage.
/// </summary>
public sealed class MixedTaskExecutor : ITaskExecutor
{
    private readonly LocalTaskExecutor _local;
    private readonly ITaskExecutor _remote;
    private readonly LocalStorage _storage;
    private readonly IStorageTransit _transit;

    /// <param name="remote">What the tasks set to run on a worker are sent to.</param>
    /// <param name="transit">Where the storage they read and write crosses over to the worker.</param>
    public MixedTaskExecutor(TaskLoaders tasks, LocalStorage storage, ITaskExecutor remote, IStorageTransit transit)
    {
        _remote = remote;
        _storage = storage;
        _transit = transit;
        _local = new LocalTaskExecutor(tasks, storage, new WorkflowExecutor(this));
    }

    public Task<TaskInstance> ExecuteAsync(
        BaseAutomationTask task,
        TaskInstance instance,
        TaskInstancesProgress? progress = null,
        CancellationToken? cancellation = null)
    {
        if (task.Settings.Execution == EnumExecutionMode.Local)
            return _local.ExecuteAsync(task, instance, progress, cancellation);

        return ExecuteRemotelyAsync(task, instance, progress, cancellation);
    }

    private async Task<TaskInstance> ExecuteRemotelyAsync(
        BaseAutomationTask task,
        TaskInstance instance,
        TaskInstancesProgress? progress,
        CancellationToken? cancellation)
    {
        try
        {
            await StorageTransfer.SendAsync(_storage, instance.Parameters, _transit, instance.Id);
            instance = await _remote.ExecuteAsync(task, instance, progress, cancellation);
            // Whatever it came to : what is left in transit is taken, the storage its output points at with it.
            await StorageTransfer.ReceiveAsync(_transit, instance.Id, _storage);
        }
        catch (Exception ex)
        {
            instance.State = EnumTaskState.Failed;
            instance.Output = JToken.FromObject(ex.ToString());
        }

        return instance;
    }
}
