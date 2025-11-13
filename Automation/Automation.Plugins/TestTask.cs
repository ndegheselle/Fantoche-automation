using Automation.Plugins.Shared;

namespace Automation.Plugins
{
    public class TestResult
    {
        public int Value { get; set; }
        public string Message { get; set; } = "";
    }

    public class TestParameters
    {
        public string Message { get; set; } = "";
        public int Value { get; set; }
        public int Add { get; set; }
    }

    public class TestTask : BaseTask<TestParameters, TestResult>
    {
        public override Task<TestResult> DoAsync(TestParameters parameters, IProgress<TaskNotification>? progress = null)
        {
            return Task.FromResult(new TestResult()
            {
                Value = parameters.Value + parameters.Add,
                Message = $"{parameters.Message} -> task"
            });
        }
    }
}
