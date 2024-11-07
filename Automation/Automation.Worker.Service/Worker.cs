using Automation.Dal.Models;
using Automation.Dal.Repositories;
using Automation.Realtime;
using Automation.Realtime.Clients;
using Automation.Realtime.Models;
using Automation.Server.Shared.Packages;
using Automation.Shared.Data;
using Automation.Worker.Service.Business;
using MongoDB.Driver;

namespace Automation.Worker.Service
{
    public class Worker : BackgroundService
    {
        private readonly TaskIntancesRepository _repository;

        private readonly WorkerInstance _instance;
        private readonly TaskExecutor _executor;
        private readonly WorkerRealtimeClient _workerClient;

        private TaskCompletionSource? _waitingForTask;

        public Worker(
            ILogger<Worker> logger,
            WorkerInstance instance,
            IMongoDatabase database,
            RedisConnectionManager redis,
            IPackageManagement packageManagement)
        {
            _instance = instance;
            _repository = new TaskIntancesRepository(database);
            _executor = new TaskExecutor(redis.Connection, packageManagement);
            _workerClient = new WorkersRealtimeClient(redis.Connection).ByWorker(_instance.Id);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _workerClient.Tasks.SubscribeQueue(() => 
                _waitingForTask?.SetResult()
            );
            while (!stoppingToken.IsCancellationRequested)
            {
                // Execute all the queued tasks
                Guid? taskId;
                do
                {
                    taskId = await _workerClient.Tasks.DequeueAsync();
                    if (taskId != null)
                    {
                        await Execute(taskId.Value);
                    }
                } while (taskId != null);

                // Wait for a new task
                _waitingForTask = new TaskCompletionSource(stoppingToken);
                await _waitingForTask.Task;
            }
        }

        private async Task Execute(Guid taskId)
        {
            TaskInstance? instance = await _repository.GetByIdAsync(taskId) ??
                throw new ArgumentException($"Unknow task instance id '{taskId}'");

            instance.State = EnumTaskState.Progress;
            instance.StartDate = DateTime.Now;
            await _repository.UpdateAsync(instance.Id, instance);

            instance = await _executor.ExecuteAsync(instance);
            instance.EndDate = DateTime.Now;
            await _repository.UpdateAsync(instance.Id, instance);
        }
    }
}
