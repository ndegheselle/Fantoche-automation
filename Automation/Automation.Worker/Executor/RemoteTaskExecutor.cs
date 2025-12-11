using Automation.Dal;
using Automation.Dal.Repositories;
using Automation.Models.Work;
using Automation.Plugins.Shared;
using Automation.Realtime.Clients;
using Automation.Realtime.Models;
using Automation.Shared.Data.Task;
using Newtonsoft.Json.Linq;

namespace Automation.Worker.Executor
{
    /// <summary>
    /// Execute a task by sending it to a worker
    /// </summary>
    public class RemoteTaskExecutor : ITaskExecutor
    {
        private readonly RealtimeClients _realtime;
        private readonly TaskInstancesRepository _instanceRepo;

        public RemoteTaskExecutor(DatabaseConnection connection, RealtimeClients realtime)
        {
            _realtime = realtime;
            _instanceRepo = new TaskInstancesRepository(connection);
        }

        /// <summary>
        /// Assign an instance to an available worker
        /// </summary>
        /// <param name="taskInstance"></param>
        /// <returns></returns>
        public async Task<TaskInstance> AssignAsync(TaskInstance instance)
        {
            WorkerInstance selectedWorker = await SelectWorkerAsync();
            instance.WorkerId = selectedWorker.Id;
            Guid instanceId = await _instanceRepo.CreateOrReplaceAsync(instance); 
            _realtime.Workers.ByWorker(selectedWorker.Id).Tasks.QueueAsync(instanceId);
            return instance;
        }

        /// <summary>
        /// Select a worker for the task.
        /// </summary>
        /// <param name="task">Task to execute</param>
        /// <returns>The selected worker instance.</returns>
        /// <exception cref="Exception">No worker found.</exception>
        private async Task<WorkerInstance> SelectWorkerAsync()
        {
            IEnumerable<WorkerInstance> workers = await _realtime.Workers.GetWorkersAsync();
            return workers.MinBy(async x => await _realtime.Workers.ByWorker(x.Id).Tasks.GetQueueLengthAsync()) ??
                throw new Exception("No available worker for the task.");
        }

        /// <inheritdoc/>
        ///     Task<TaskOutput> ExecuteAsync(
        public async Task<TaskOutput> ExecuteAsync(
            BaseAutomationTask automationTask,
            JToken? input,
            IProgress<TaskNotification>? notifications = null,
            CancellationToken? cancellation = null)
        {
            /*
            instance = await AssignAsync(instance);

            await _realtime.States.WaitStateAsync(instance.Id, EnumTaskState.Finished);
            // Get the updated instance from the repository
            return await _instanceRepo.GetByIdAsync(instance.Id) ??
                throw new Exception($"Unable to find the instance for the id '{instance.Id}'");
            */
            throw new NotImplementedException();
        }
    }
}