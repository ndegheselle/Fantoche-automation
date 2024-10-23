using Automation.Dal.Models;
using Automation.Dal.Repositories;
using Automation.Realtime;
using Automation.Realtime.Clients;
using Automation.Realtime.Models;
using Automation.Shared.Data;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace Automation.Worker.Service
{
    public class Worker : BackgroundService
    {
        private readonly SemaphoreSlim _executeLock = new SemaphoreSlim(1, 1);
        private readonly TaskIntanceRepository _repository;
        private readonly RedisConnectionManager _redis;
        private readonly WorkerInstance _instance;
        private readonly IConfiguration _configuration;

        private readonly TaskWorker _worker;

        public Worker(IConfiguration configuration, IMongoDatabase database, RedisConnectionManager redis)
        {
            _configuration = configuration;
            _redis = redis;
            _repository = new TaskIntanceRepository(database);
            _worker = new TaskWorker(redis);
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
            await HandlePendingTasks();
            ListenToNewTasks();

            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
            await workerClient.UnregisterAsync(_instance);
        }

        private async Task HandlePendingTasks()
        {
            IEnumerable<TaskInstance>? tasks = await _repository.GetByWorkerAndStateAsync(_instance.Id, [EnumTaskState.Pending, EnumTaskState.Progress]);

            // XXX : parallel execution ?
            foreach (var task in tasks)
                await Execute(task);
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
                await _repository.UpdateAsync(instance.Id, instance);
                instance.State = await _worker.ExecuteAsync(instance);
                await _repository.UpdateAsync(instance.Id, instance);
            }
            finally
            {
                _executeLock.Release();
            }
        }
    }
}
