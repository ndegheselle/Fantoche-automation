
using Automation.Realtime.Clients;
using Automation.Realtime;
using Microsoft.Extensions.Hosting;
using Automation.Realtime.Models;

namespace Automation.Api.Worker.Services
{
    public class RealtimeService : BackgroundService
    {
        private readonly RedisConnectionManager _redis;
        private readonly WorkerInstance _instance;

        public RealtimeService(RedisConnectionManager redis)
        {
            _redis = redis;
            // TODO : load config from environement
            _instance = new WorkerInstance()
            {
                Id = Guid.NewGuid().ToString(),
            };
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            RegisterWorker();
            ListenToNewTasks();

            return Task.CompletedTask;
        }

        private void ListenToNewTasks()
        {
            TasksRealtimeClient client = new TasksRealtimeClient(_redis);
            client.Subscribe(_instance.Id, OnTaskAssigned);
        }

        private async void RegisterWorker()
        {
            WorkerRealtimeClient client = new WorkerRealtimeClient(_redis);
            await client.AddWorkerAsync(_instance);
        }

        private void OnTaskAssigned(Guid taskId)
        {
            // Load task from mongo
            // Do
            throw new NotImplementedException();
        }
    }
}
