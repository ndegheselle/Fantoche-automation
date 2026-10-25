using Fantoche.Plugins.Shared;

namespace Fantoche.Worker.Executor;

internal sealed class TaskRuntime : ITaskRuntime
{
    public bool IsOutputDeactivated { get; private set; }

    public TaskRuntime(IProgress<TaskNotification>? progress, IStorage storage, IUserInterface? userInterface)
    {
        Progress = progress;
        Storage = storage;
        UserInterface = userInterface;
    }

    public IProgress<TaskNotification>? Progress { get; }

    public IStorage Storage { get; }

    public IUserInterface? UserInterface { get; }

    public void DeactivateOutput(bool deactivate = true)
    {
        IsOutputDeactivated = deactivate;
    }
}
