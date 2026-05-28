using Automation.Plugins.Shared;

namespace Automation.Plugins;

public class TestDelayParameters
{
    public int DelayMs { get; set; } = 500;
}

public class TestDelay : BasePassThroughTask<TestDelayParameters>
{
    public override async Task DoAsync(TestDelayParameters parameters, ITaskRuntime runtime, CancellationToken? cancellation = null)
    {
        await Task.Delay(parameters.DelayMs, cancellation ?? CancellationToken.None);
    }
}