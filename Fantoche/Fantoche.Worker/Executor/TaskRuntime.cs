using Fantoche.Plugins.Shared;

namespace Fantoche.Worker.Executor;

internal sealed class TaskRuntime : ITaskRuntime
{
    public bool IsOutputDeactivated { get; private set; }

    public TaskRuntime(IProgress<TaskNotification>? progress, IStorage storage, IUserPrompt? userPrompt)
    {
        Progress = progress;
        Storage = storage;
        UserPrompt = userPrompt;
    }

    public IProgress<TaskNotification>? Progress { get; }

    public IStorage Storage { get; }

    public IUserPrompt? UserPrompt { get; }

    public void DeactivateOutput(bool deactivate = true)
    {
        IsOutputDeactivated = deactivate;
    }
}
