using Automation.Dal.Models;
using Automation.Dal.Repositories;
using Automation.Plugins.Shared;
using Automation.Realtime;
using Automation.Realtime.Clients;
using Automation.Realtime.Models;
using Automation.Shared.Packages;
using MongoDB.Driver;
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
        private readonly WorkersRealtimeClient _workersClient;
        // To send task progress to clients
        private readonly IPackageManagement _packages;

        public RemoteTaskExecutor(IMongoDatabase database, RedisConnectionManager connection, IPackageManagement packageManagement)
        {
            _redis = connection;
            _packages = packageManagement;
            _instanceRepo = new TaskIntancesRepository(database);
            _workersClient = new WorkersRealtimeClient(_redis.Connection);
        }

        /// <summary>
        /// Create a task instance for a specific task
        /// </summary>
        /// <param name="taskId"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<TaskInstance> AssignAsync(Guid taskId, TaskContext context)
        {
            TaskInstance taskInstance = new TaskInstance(taskId, context);
            WorkerInstance selectedWorker = await SelectWorkerAsync(taskInstance);
            taskInstance.WorkerId = selectedWorker.Id;
            await _instanceRepo.CreateAsync(taskInstance);
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
            TaskInstance instance = await AssignAsync(automationTask.Id, context);
            var tasksClient = new TasksRealtimeClient(_redis.Connection, instance.Id);

            tasksClient.Lifecycle.WaitStateAsync()

            await Task.Run(() =>
            {
                TaskCompletionSource completion = new TaskCompletionSource();
                tasksClient.Lifecycle.Subscribe((state) =>
                {
                    if (state == EnumTaskState.Finished)
                    {
                        completion.SetResult();
                    }
                    else if (state == EnumTaskState.Failed)
                    {
                        completion.SetException(new Exception("Task failed"));
                    }
                });
                return completion.Task;
            });

            tasksClient.Lifecycle.Unsubscribe();

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
