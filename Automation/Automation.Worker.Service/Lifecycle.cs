using Automation.Realtime;
using Automation.Realtime.Clients;
using Automation.Realtime.Models;

namespace Automation.Worker.Service
{
    /// <summary>
    /// Register the worker and send heartbeat reguraly
    /// </summary>
    public class Lifecycle : BackgroundService
    {
        private readonly TimeSpan _heartbeatInterval = TimeSpan.FromSeconds(30);
        private readonly RedisConnectionManager _redis;
        private readonly WorkerInstance _instance;

        public Lifecycle(WorkerInstance instance, RedisConnectionManager redis)
        {
            _redis = redis;
            _instance = instance;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            WorkersRealtimeClient workersClient = new WorkersRealtimeClient(_redis.Connection);
            await workersClient.UpdateWorkerAsync(_instance);
            while (!stoppingToken.IsCancellationRequested)
            {
                await workersClient.ByWorker(_instance.Id).UpdateHeartbeatAsync();
                await Task.Delay(_heartbeatInterval, stoppingToken);
            }
            await workersClient.RemoveWorkerAsync(_instance.Id);
        }
    }
}
