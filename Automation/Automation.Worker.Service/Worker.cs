using Automation.Dal.Models;
using Automation.Dal.Repositories;
using Automation.Realtime;
using Automation.Realtime.Clients;
using Automation.Realtime.Models;
using Automation.Server.Shared;
using Automation.Shared.Data;
using Automation.Worker.Service.Business;
using MongoDB.Driver;

namespace Automation.Worker.Service
{
    public class Worker : BackgroundService
    {
        private readonly TaskIntanceRepository _repository;
        private readonly RedisConnectionManager _redis;

        private readonly WorkerInstance _instance;
        private readonly TaskExecutor _executor;

        private readonly BackgroundTaskQueue<TaskInstance> _queue;

        public Worker(WorkerInstance instance, IMongoDatabase database, RedisConnectionManager redis)
        {
            _redis = redis;
            _repository = new TaskIntanceRepository(database);
            _executor = new TaskExecutor(redis);
            _instance = instance;
            _queue = new BackgroundTaskQueue<TaskInstance>(100);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            ListenToNewTasks();
            while (!stoppingToken.IsCancellationRequested)
            {
                var workItem = await _queue.DequeueAsync(stoppingToken);
                await Execute(workItem);
            }
        }

        private void ListenToNewTasks()
        {
            TasksRealtimeClient client = new TasksRealtimeClient(_redis);
            client.Subscribe(_instance.Id, OnTaskAssigned);
        }

        private async void OnTaskAssigned(Guid taskId)
        {
            TaskInstance? instance = await _repository.GetByIdAsync(taskId) 
                ?? throw new ArgumentException($"Unknow task instance id '{taskId}'");
            await _queue.QueueAsync(instance);
        }

        private async Task Execute(TaskInstance instance)
        {
            instance.State = EnumTaskState.Progress;
            instance.StartDate = DateTime.Now;
            await _repository.UpdateAsync(instance.Id, instance);

            instance.State = await _executor.ExecuteAsync(instance);
            instance.EndDate = DateTime.Now;
            await _repository.UpdateAsync(instance.Id, instance);
        }
    }
}
