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
    public class Worker : BackgroundService
    {
        private readonly IConfiguration _configuration;
        // TODO : use a proper queue
        private readonly SemaphoreSlim _executeLock = new SemaphoreSlim(1, 1);
        private readonly TimeSpan _heartbeatInterval = TimeSpan.FromSeconds(30);

        private readonly TaskIntanceRepository _repository;
        private readonly RedisConnectionManager _redis;

        private readonly WorkerInstance _instance;
        private readonly TaskExecutor _executor;

        public Worker(IConfiguration configuration, IMongoDatabase database, RedisConnectionManager redis)
        {
            _configuration = configuration;
            _redis = redis;
            _repository = new TaskIntanceRepository(database);
            _executor = new TaskExecutor(redis);
            _instance = LoadConfig();
        }

        private WorkerInstance LoadConfig()
        {
            return new WorkerInstance()
            {
                Id = _configuration["WORKER_ID"] ?? Guid.NewGuid().ToString(),
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            WorkerRealtimeClient workerClient = new WorkerRealtimeClient(_redis);

            await workerClient.RegisterAsync(_instance);
            ListenToNewTasks();

            // Update heartbeat while the service is up
            while (!stoppingToken.IsCancellationRequested)
            {
                await workerClient.UpdateHeartbeatAsync(_instance.Id);
                await Task.Delay(_heartbeatInterval, stoppingToken);
            }

            await workerClient.UnregisterAsync(_instance.Id);
        }

        private void ListenToNewTasks()
        {
            TasksRealtimeClient client = new TasksRealtimeClient(_redis);
            client.Subscribe(_instance.Id, OnTaskAssigned);
        }

        private async void OnTaskAssigned(Guid taskId)
        {
            TaskInstance? instance = await _repository.GetByIdAsync(taskId);

            if (instance == null)
                throw new Exception($"Unknow task instance id '{taskId}'");
            await Execute(instance);
        }

        private async Task Execute(TaskInstance instance)
        {
            try
            {
                await _executeLock.WaitAsync();
                instance.State = EnumTaskState.Progress;
                instance.StartDate = DateTime.Now;
                await _repository.UpdateAsync(instance.Id, instance);

                instance.State = await _executor.ExecuteAsync(instance);
                instance.EndDate = DateTime.Now;
                await _repository.UpdateAsync(instance.Id, instance);
            }
            finally
            {
                _executeLock.Release();
            }
        }
    }
}
