using Fantoche.Plugins.Shared;

namespace Fantoche.Worker.Executor;

internal sealed class TaskRuntime : ITaskRuntime
{
    public bool IsOutputDeactivated { get; private set; }

    public TaskRuntime(IProgress<TaskNotification>? progress, IStorage storage)
    {
        Progress = progress;
        Storage = storage;
    }

    public IProgress<TaskNotification>? Progress { get; }

    public IStorage Storage { get; }

    public void DeactivateOutput(bool deactivate = true)
    {
        IsOutputDeactivated = deactivate;
    }
}
