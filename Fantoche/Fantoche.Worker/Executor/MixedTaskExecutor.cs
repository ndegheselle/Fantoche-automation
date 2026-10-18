using Fantoche.Plugins.Shared;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Scoped;
using Fantoche.Worker.Packages;
using Newtonsoft.Json.Linq;

namespace Fantoche.Worker.Executor;

/// <summary>
/// Run each task where its <see cref="TaskSettings.Execution"/> says : in this process or on a
/// worker. A workflow run here walks its nodes through this executor, so each node is sent where
/// its own setting says.
/// </summary>
public sealed class MixedTaskExecutor : ITaskExecutor, IDisposable
{
    private readonly LocalTaskExecutor _local;
    private readonly ITaskExecutor? _remote;

    /// <param name="remote">What the remote tasks are run with, null when there is nowhere to send them.</param>
    public MixedTaskExecutor(IPackageManagement packageManagement, IStorage storage, ITaskExecutor? remote)
    {
        _remote = remote;
        _local = new LocalTaskExecutor(packageManagement, storage, new WorkflowExecutor(this));
    }

    public void Dispose() => _local.Dispose();

    public Task<TaskInstance> ExecuteAsync(
        BaseAutomationTask task,
        TaskInstance instance,
        TaskInstancesProgress? progress = null,
        CancellationToken? cancellation = null)
    {
        if (task.Settings.Execution == EnumExecutionMode.Local)
            return _local.ExecuteAsync(task, instance, progress, cancellation);

        if (_remote != null)
            return _remote.ExecuteAsync(task, instance, progress, cancellation);

        // Running it here instead would hide that the setting is not honored.
        instance.State = EnumTaskState.Failed;
        instance.Output = JToken.FromObject($"The task '{task.Metadata.Name}' runs on a worker, but no supervisor is configured.");
        return Task.FromResult(instance);
    }
}
