using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Scoped;

namespace Fantoche.Worker.Executor;

/// <summary>
/// Run a task or a workflow, wherever it ends up running.
/// </summary>
public interface ITaskExecutor
{
    /// <summary>
    /// Run [task] with [instance] and return it once finished. A failure of the task doesn't throw,
    /// it is the <see cref="TaskInstance.State"/> of the returned instance.
    /// </summary>
    Task<TaskInstance> ExecuteAsync(
        BaseAutomationTask task,
        TaskInstance instance,
        TaskInstancesProgress? progress = null,
        CancellationToken? cancellation = null);
}
