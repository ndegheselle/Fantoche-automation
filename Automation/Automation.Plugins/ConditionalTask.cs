using Automation.Plugins.Shared;

namespace Automation.Plugins;

public class ConditionalParameters
{
    public bool TestDeactivate { get; set; } = true;
}

public class ConditionalTask : BasePassThroughTask<ConditionalParameters>
{
    public override async Task DoAsync(ConditionalParameters parameters, ITaskRuntime runtime, CancellationToken? cancellation = null)
    {
        if (parameters.TestDeactivate)
            runtime.DeactivateOutput();
    }
}