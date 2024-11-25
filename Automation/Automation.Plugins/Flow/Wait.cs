using Automation.Plugins.Shared;

namespace Automation.Plugins.Flow
{
    public class WaitDelay : IResultsTask
    {
        public IProgress? Progress { get; set; }
        public Dictionary<string, object> Results { get; private set; } = [];

        public async Task ExecuteAsync(TaskContext context)
        {
            Results = new Dictionary<string, object>()
            {
                { "test", "Wow that's some data." }
            };
            await Task.Delay(5000);
        }
    }
}
