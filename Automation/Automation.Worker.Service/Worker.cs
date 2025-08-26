using Automation.Dal;
using Automation.Dal.Models;
using Automation.Dal.Repositories;
using Automation.Plugins.Shared;
using Automation.Realtime;
using Automation.Realtime.Clients;
using Automation.Realtime.Models;
using Automation.Shared.Data;
using Automation.Worker.Executor;
using MongoDB.Driver;

namespace Automation.Worker.Service
{
    public class Worker : BackgroundService
    {
        private readonly TaskIntancesRepository _instanceRepo;
        private readonly TasksRepository _taskRepo;

        private readonly WorkerInstance _instance;
        private readonly LocalTaskExecutor _executor;
        private readonly WorkerRealtimeClient _workerClient;

        private TaskCompletionSource? _waitingForTask;

        public Worker(
            ILogger<Worker> logger,
            WorkerInstance instance,
            DatabaseConnection connection,
            RedisConnectionManager redis,
            Packages.IPackageManagement packageManagement)
        {
            _instance = instance;
            _instanceRepo = new TaskIntancesRepository(connection);
            _taskRepo = new TasksRepository(connection);
            _executor = new LocalTaskExecutor(connection, packageManagement);
            _workerClient = new WorkersRealtimeClient(redis.Connection).ByWorker(_instance.Id);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _workerClient.Tasks.Subscribe(new Progress<Guid>((id) => _waitingForTask?.SetResult()));
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

        private async Task Execute(Guid instanceId)
        {
            TaskInstance instance = await _instanceRepo.GetByIdAsync(instanceId);

            instance.State = EnumTaskState.Progressing;
            instance.StartedAt = DateTime.UtcNow;
            await _instanceRepo.UpdateAsync(instance.Id, instance);

            instance = await _executor.ExecuteAsync(instance);

            instance.FinishedAt = DateTime.UtcNow;
            await _instanceRepo.UpdateAsync(instance.Id, instance);
        }
    }
}
