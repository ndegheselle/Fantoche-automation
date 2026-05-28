using Automation.Plugins.Shared;

namespace Automation.Worker.Executor;

/// <summary>
/// Engine-side <see cref="ITaskRuntime"/> implementation: holds the running graph node,
/// accumulates the names of activated output branches, and (on demand) translates them
/// into the connector GUIDs the workflow executor uses to drive downstream propagation.
/// </summary>
internal sealed class TaskRuntime : ITaskRuntime
{
    public bool IsOutputDeactivated { get; private set; }

    public TaskRuntime(IProgress<TaskNotification>? progress)
    {
        Progress = progress;
    }

    public IProgress<TaskNotification>? Progress { get; }

    public void DeactivateOutput(bool deactivate = true)
    {
        IsOutputDeactivated = deactivate;
    }
}
