using Automation.Plugins.Shared;

namespace Automation.Plugins;

public class PassThroughParameters
{
    public string Label { get; set; } = "";
}

/// <summary>
/// Does nothing to the data flow: the downstream context transparently reads the
/// upstream task's output. Only emits a progress notification for observability.
/// </summary>
public class PassThroughTask : BasePassThroughTask<PassThroughParameters>
{
    public override Task DoAsync(PassThroughParameters parameters, ITaskRuntime runtime, CancellationToken? cancellation = null)
    {
        runtime.Progress?.Report(new TaskNotification
        {
            State = EnumTaskNotificationState.Info,
            Message = $"Passing through: {parameters.Label}"
        });
        return Task.CompletedTask;
    }
}
