using Automation.Dal.Models;
using Automation.Dal.Repositories;
using Automation.Plugins.Shared;
using Automation.Realtime;
using Automation.Realtime.Clients;
using Automation.Realtime.Models;
using Automation.Shared.Packages;
using MongoDB.Bson;
using MongoDB.Driver;
using NuGet.Protocol.Core.Types;
using StackExchange.Redis;

namespace Automation.Worker
{
    /// <summary>
    /// Execute a task.
    /// </summary>
    public class RemoteTaskExecutor : ITaskExecutor
    {
        private readonly RedisConnectionManager _redis;
        private readonly TaskIntancesRepository _instanceRepo;
        private readonly TasksRepository _taskRepo;

        // To send task progress to clients
        private readonly IPackageManagement _packages;
        private TasksRealtimeClient? _tasksClient;

        public RemoteTaskExecutor(IMongoDatabase database, RedisConnectionManager connection, IPackageManagement packageManagement)
        {
            _instanceRepo = new TaskIntancesRepository(database);
            _taskRepo = new TasksRepository(database);
            _redis = connection;
            _packages = packageManagement;
        }

        /// <summary>
        /// Create a task instance for a specific task
        /// </summary>
        /// <param name="taskId"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public async Task<TaskInstance> AssignAsync(AutomationTask task, string? settings)
        {
            if (task.Package == null)
                throw new ArgumentNullException(nameof(task));

            TaskInstance taskInstance = new TaskInstance(task.Id, task.Package, new InstanceContext() { Settings = settings });
            WorkerInstance selectedWorker = await SelectWorkerAsync(taskInstance);
            taskInstance.WorkerId = selectedWorker.Id;
            await _repository.CreateAsync(taskInstance);
            _workersClient.ByWorker(selectedWorker.Id).Tasks.QueueAsync(taskInstance.Id);
            return taskInstance;
        }

        /// <summary>
        /// Select a worker for the task.
        /// </summary>
        /// <param name="task">Task to execute</param>
        /// <returns>The selected worker instance.</returns>
        /// <exception cref="Exception">No worker found.</exception>
        private async Task<WorkerInstance> SelectWorkerAsync(TaskInstance task)
        {
            IEnumerable<WorkerInstance> workers = await _workersClient.GetWorkersAsync();
            return workers.MinBy(async x => await _workersClient.ByWorker(x.Id).Tasks.GetQueueLengthAsync())
                ?? throw new Exception("No available worker for the task.");
        }


        public async Task<EnumTaskState> ExecuteAsync(AutomationTask automationTask, TaskContext context, IProgress<TaskProgress> progress)
        {
            try
            {
                // Create task instance
                // Wait for assigned worker to finish this specific task
                // Get the updated instance with updated context
                // Return the result
            }
            catch
            { }

            return EnumTaskState.Failed;
        }
    }
}
