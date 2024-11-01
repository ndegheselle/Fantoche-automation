using Automation.Dal.Models;
using Automation.Dal.Repositories;
using Automation.Realtime;
using Automation.Realtime.Clients;
using Automation.Realtime.Models;
using Automation.Server.Shared.Packages;
using Automation.Shared.Data;
using Automation.Worker.Service.Business;
using MongoDB.Driver;
using System.Threading.Tasks;

namespace Automation.Worker.Service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly TaskIntanceRepository _repository;

        private readonly WorkerInstance _instance;
        private readonly TaskExecutor _executor;
        private readonly WorkerRealtimeClient _workerClient;
        private readonly TasksRealtimeClient _taskClient;

        private TaskCompletionSource? _waitingForTask;

        public Worker(
            ILogger<Worker> logger,
            WorkerInstance instance,
            IMongoDatabase database,
            RedisConnectionManager redis,
            IPackageManagement packageManagement)
        {
            _logger = logger;
            _repository = new TaskIntanceRepository(database);
            _executor = new TaskExecutor(redis, packageManagement);
            _workerClient = new WorkerRealtimeClient(redis);
            _taskClient = new TasksRealtimeClient(redis);
            _instance = instance;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _taskClient.SubscribeQueue(_instance.Id, () => 
                _waitingForTask?.SetResult()
            );
            while (!stoppingToken.IsCancellationRequested)
            {
                // Execute all the queued tasks
                Guid? taskId;
                do
                {
                    taskId = await _taskClient.DequeueTaskAsync(_instance.Id);
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

            _logger.LogDebug($"Starting instance {instance.TaskId} - {DateTime.Now}");
            instance.State = EnumTaskState.Progress;
            instance.StartDate = DateTime.Now;
            await _repository.UpdateAsync(instance.Id, instance);

            instance = await _executor.ExecuteAsync(instance);
            instance.EndDate = DateTime.Now;
            await _repository.UpdateAsync(instance.Id, instance);

            _logger.LogDebug($"Ending instance {instance.TaskId} - {DateTime.Now}");
        }
    }
}
