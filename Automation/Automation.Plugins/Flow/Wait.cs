using Automation.Plugins.Shared;

namespace Automation.Plugins.Flow
{
    public class WaitDelay : ITask
    {
        public IProgress? Progress { get; set; }
        public async Task<dynamic?> ExecuteAsync(TaskContext context)
        {
            await Task.Delay(5000);
            return Task.FromResult<dynamic?>(new { Test = "Wow that should be saved." });
        }
    }
}
