using Fantoche.Plugins.Shared;

namespace Fantoche.Plugins;

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