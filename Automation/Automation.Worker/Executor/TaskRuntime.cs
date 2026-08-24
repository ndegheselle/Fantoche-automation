using Automation.Plugins.Shared;

namespace Automation.Worker.Executor;

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
