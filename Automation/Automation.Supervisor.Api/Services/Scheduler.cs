namespace Automation.Supervisor.Api.Services
{
    public class Scheduler : BackgroundService
    {
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTimeOffset.Now;
                var next = _expression.GetNextOccurrence(now, _timeZoneInfo);
                if (!next.HasValue) continue;

                var delay = next.Value - now;
                await Task.Delay(delay, cancellationToken);

                if (cancellationToken.IsCancellationRequested) continue;

                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    await DoWork(scope, cancellationToken);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, nameof(ExecuteAsync));
                }
            }
        }
    }
}
