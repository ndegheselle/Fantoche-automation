using Automation.Dal;
using Automation.Dal.Repositories;
using Automation.Models.Work;
using Automation.Realtime;
using Automation.Realtime.Clients;
using Automation.Realtime.Models;
using Automation.Shared.Data.Task;
using Automation.Worker.Executor;
using Automation.Worker.Packages;

namespace Automation.Worker.Service
{
    public class Worker : BackgroundService
    {
        private readonly TaskInstancesRepository _instanceRepo;
        private readonly LocalTaskExecutor _executor;
        private readonly WorkerRealtimeClient _workerClient;

        private TaskCompletionSource? _waitingForTask;

        public Worker(
            ILogger<Worker> logger,
            WorkerInstance instance,
            DatabaseConnection connection,
            RedisConnectionManager redis,
            IPackageManagement packageManagement)
        {
            _instanceRepo = new TaskInstancesRepository(connection);
            _executor = new LocalTaskExecutor(connection, packageManagement);
            _workerClient = new WorkersRealtimeClient(redis.Connection).ByWorker(instance.Id);
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
                        TaskInstance instance = await _instanceRepo.GetByIdAsync(taskId.Value);
                        await _executor.ExecuteAsync(instance);
                    }
                } while (taskId != null);

                // Wait for a new task
                _waitingForTask = new TaskCompletionSource(stoppingToken);
                await _waitingForTask.Task;
            }
        }
    }
}