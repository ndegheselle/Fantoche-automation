using Automation.Plugins.Shared;

namespace Automation.Plugins;

public class TestTaskNew : BaseTask<TestParameters, TestResult>
{
    public override Task<TestResult> DoAsync(TestParameters parameters, ITaskRuntime runtime, CancellationToken? cancellation = null)
    {
        return Task.FromResult(new TestResult()
        {
            Value = parameters.Value + parameters.Add,
            Message = $"{parameters.Message} -> task"
        });
    }
}