namespace Automation.Supervisor.Api.Services
{
    public class Scheduler : BackgroundService
    {
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }
    }
}
