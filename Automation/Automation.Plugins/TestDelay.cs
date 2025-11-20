using Automation.Plugins.Shared;

namespace Automation.Plugins;

public class TestDelayParameters
{
    public int DelayMs { get; set; } = 500;
}

public class TestDelay : BaseTask<TestDelayParameters>
{
    public override async Task DoAsync(TestDelayParameters parameters, IProgress<TaskNotification>? progress = null, CancellationToken? cancellation = null)
    {
        await Task.Delay(parameters.DelayMs, cancellation ?? CancellationToken.None);
    }
}