using Automation.Dal.Models;
using Automation.Dal.Repositories;
using Automation.Realtime;
using Automation.Realtime.Clients;
using Automation.Realtime.Models;
using Automation.Shared.Data;
using Automation.Worker.Service.Business;
using MongoDB.Driver;

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
            WorkersRealtimeClient workerClient = new WorkersRealtimeClient(_redis);
            await workerClient.UpdateWorkerAsync(_instance);
            while (!stoppingToken.IsCancellationRequested)
            {
                await workerClient.UpdateHeartbeatAsync(_instance.Id);
                await Task.Delay(_heartbeatInterval, stoppingToken);
            }
            await workerClient.RemoveWorkerAsync(_instance.Id);
        }
    }
}
