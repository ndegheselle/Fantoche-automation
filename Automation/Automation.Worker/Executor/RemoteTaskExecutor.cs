using Automation.Dal.Models;
using Automation.Dal.Repositories;
using Automation.Plugins.Shared;
using Automation.Realtime.Clients;
using Automation.Realtime.Models;
using MongoDB.Driver;

namespace Automation.Worker.Executor
{
    /// <summary>
    /// Execute a task by sending it to a worker
    /// </summary>
    public class RemoteTaskExecutor : ITaskExecutor
    {
        private readonly RealtimeClients _realtime;
        private readonly TaskIntancesRepository _instanceRepo;

        public RemoteTaskExecutor(IMongoDatabase database, RealtimeClients realtime)
        {
            _realtime = realtime;
            _instanceRepo = new TaskIntancesRepository(database);
        }

        /// <summary>
        /// Assign an instance to an available worker
        /// </summary>
        /// <param name="taskInstance"></param>
        /// <returns></returns>
        public async Task<AutomationTaskInstance> AssignAsync(AutomationTaskInstance taskInstance)
        {
            WorkerInstance selectedWorker = await SelectWorkerAsync(taskInstance);
            taskInstance.WorkerId = selectedWorker.Id;
            await _instanceRepo.CreateAsync(taskInstance);
            _realtime.Workers.ByWorker(selectedWorker.Id).Tasks.QueueAsync(taskInstance.Id);
            return taskInstance;
        }

        /// <summary>
        /// Select a worker for the task.
        /// </summary>
        /// <param name="task">Task to execute</param>
        /// <returns>The selected worker instance.</returns>
        /// <exception cref="Exception">No worker found.</exception>
        private async Task<WorkerInstance> SelectWorkerAsync(AutomationTaskInstance task)
        {
            IEnumerable<WorkerInstance> workers = await _realtime.Workers.GetWorkersAsync();
            return workers.MinBy(async x => await _realtime.Workers.ByWorker(x.Id).Tasks.GetQueueLengthAsync()) ??
                throw new Exception("No available worker for the task.");
        }

        /// <inheritdoc/>
        public async Task<AutomationTaskInstance> ExecuteAsync(
            AutomationTaskInstance instance,
            IProgress<TaskInstanceNotification>? progress = null)
        {
            instance = await AssignAsync(instance);
            try
            {
                if (progress != null)
                    _realtime.Notifications.Subscribe(instance.Id, progress);

                EnumTaskState finishedState = await _realtime.Lifecycle.WaitStateAsync(instance.Id, EnumTaskState.Finished);
                // Udpate the context with the instance parameters stored in the database
                AutomationTaskInstance finishedInstance = await _instanceRepo.GetByIdAsync(instance.Id) ??
                    throw new Exception($"Unable to find the instance for the id '{instance.Id}'");
                instance.Parameters.ContextJson = finishedInstance.Parameters.ContextJson;
            } catch
            {
                instance.State = EnumTaskState.Failed;
            }

            return instance;
        }
    }
}
