using Automation.Plugins.Shared;

namespace Automation.Plugins;

public class LoopGateParameters
{
    public int Value { get; set; }
    public int Max { get; set; }

    /// <summary>
    /// Keep the output while the value is under the max (loop body) or, when false, while it
    /// reached the max (loop exit). Two gates with the opposite mode on the same output form
    /// the two sides of a loop.
    /// </summary>
    public bool WhileUnder { get; set; } = true;
}

/// <summary>
/// Pass-through gate closing a branch depending on a value : lets a loop end on the data it
/// produces, the branch dying as soon as the output is deactivated.
/// </summary>
public class LoopGateTask : BasePassThroughTask<LoopGateParameters>
{
    public override Task DoAsync(LoopGateParameters parameters, ITaskRuntime runtime, CancellationToken? cancellation = null)
    {
        bool isOpen = parameters.WhileUnder
            ? parameters.Value < parameters.Max
            : parameters.Value >= parameters.Max;

        if (!isOpen)
            runtime.DeactivateOutput();

        runtime.Progress?.Report(new TaskNotification
        {
            State = EnumTaskNotificationState.Info,
            Message = $"Gate {(isOpen ? "open" : "closed")} ({parameters.Value}/{parameters.Max})"
        });
        return Task.CompletedTask;
    }
}
