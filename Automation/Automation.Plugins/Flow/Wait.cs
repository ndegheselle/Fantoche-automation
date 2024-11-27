using Automation.Plugins.Shared;
using System.Text.Json;

namespace Automation.Plugins.Flow
{
    public class WaitDelaySettings
    {
        public int DelayMs { get; set; } = 500;
    }

    public class WaitDelay : IResultsTask
    {
        public IProgress? Progress { get; set; }
        public Dictionary<string, object> Results { get; private set; } = [];

        public async Task ExecuteAsync(TaskContext context)
        {
            WaitDelaySettings settings = new WaitDelaySettings();
            if (context.SettingsJson != null)
                settings = JsonSerializer.Deserialize<WaitDelaySettings>(context.SettingsJson) ?? new WaitDelaySettings();

            Results = new Dictionary<string, object>()
            {
                { "TotalDelay", $"Delay {settings.DelayMs}" }
            };
            await Task.Delay(settings.DelayMs);
        }
    }
}
