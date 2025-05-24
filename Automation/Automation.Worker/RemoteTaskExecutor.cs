using Automation.Dal.Models;
using Automation.Dal.Repositories;
using Automation.Plugins.Shared;
using Automation.Realtime;
using Automation.Realtime.Clients;
using Automation.Realtime.Models;
using Automation.Shared.Data;
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

        public RemoteTaskExecutor(IMongoDatabase database, RedisConnectionManager connection)
        {
            _redis = connection;
            _instanceRepo = new TaskIntancesRepository(database);
            _workersClient = new WorkersRealtimeClient(_redis.Connection);
        }

        /// <summary>
        /// Create a task instance for a specific task
        /// </summary>
        /// <param name="taskId"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task<TaskInstance> AssignAsync(Guid taskId, TaskParameters context)
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

        /// <inheritdoc />
        public async Task<TaskInstance> ExecuteAsync(AutomationTask automationTask, TaskParameters parameters, IProgress<TaskProgress>? progress)
        {
            TaskInstance instance = await AssignAsync(automationTask.Id, parameters);
            var lifecycle = new InstanceLifecycleRedisPublisher(_redis.Connection, instance.Id);
            using InstanceProgressRedisPublisher instanceProgress = new InstanceProgressRedisPublisher(_redis.Connection, instance.Id);

            try
            {
                Guid progressSubsciberId = instanceProgress.Subscribe((p, subscriberId) =>
                {
                    progress?.Report(p);
                });

                EnumTaskState finishedState = await lifecycle.WaitStateAsync(EnumTaskState.Finished);
                // Udpate the context with the instance parameters stored in the database
                TaskInstance finishedInstance = await _instanceRepo.GetByIdAsync(instance.Id) ?? throw new Exception($"Unable to find the instance for the id '{instance.Id}'");
                parameters.ContextJson = finishedInstance.Parameters.ContextJson;
            }
            catch
            { }

            return instance;
        }
    }
}
