using Automation.Dal.Models;
using Automation.Dal.Repositories;
using Automation.Realtime;
using Automation.Realtime.Clients;
using Automation.Realtime.Models;
using Automation.Shared.Data;
using MongoDB.Driver;

namespace Automation.Worker.Service
{
    public class Worker : BackgroundService
    {
        private readonly TaskIntanceRepository _repository;
        private readonly RedisConnectionManager _redis;
        private readonly WorkerInstance _instance;

        private readonly TaskWorker _worker;

        public Worker(IMongoDatabase database, RedisConnectionManager redis)
        {
            _repository = new TaskIntanceRepository(database);
            _worker = new TaskWorker(redis);
            _redis = redis;

            // TODO : load config from environement
            _instance = new WorkerInstance()
            {
                Id = Guid.NewGuid().ToString(),
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

        // TODO : lock during execution
        private async Task Execute(TaskInstance instance)
        {
            instance.State = EnumTaskState.Progress;
            await _repository.UpdateAsync(instance.Id, instance);
            instance.State = await _worker.ExecuteAsync(instance);
            await _repository.UpdateAsync(instance.Id, instance);
        }
    }
}
