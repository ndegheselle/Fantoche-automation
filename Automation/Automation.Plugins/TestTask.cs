using Automation.Plugins.Shared;

namespace Automation.Plugins
{
    public class TestResult
    {
        public string Data { get; set; } = "";
    }

    public class TestParemeters
    {
        public bool IsSomething { get; set; }
        public string Name { get; set; } = "";
        public DateTime Date { get; set; }
        public TimeSpan Duration { get; set; }
    }

    public class TestTask : BaseTask<TestParemeters, TestResult>
    {
        public override Task<TestResult> DoAsync(TestParemeters parameters, IProgress<TaskNotification>? progress = null)
        {
            TestResult result = new TestResult()
            {
                Data = $"Only name matter I guess : {parameters.Name}"
            };
            return Task.FromResult(result);
        }
    }
}
