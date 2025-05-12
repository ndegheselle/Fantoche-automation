using Automation.Dal.Models;
using Automation.Dal.Repositories;
using Automation.Realtime;
using Automation.Realtime.Clients;
using Automation.Realtime.Models;
using Automation.Shared.Data;
using MongoDB.Driver;

namespace Automation.Supervisor.Api.Business
{
    public class WorkerAssignator
    {
        private readonly TaskIntancesRepository _repository;
        private readonly WorkersRealtimeClient _workersClient;

        public WorkerAssignator(IMongoDatabase database, RedisConnectionManager redis)
        {
            _repository = new TaskIntancesRepository(database);
            _workersClient = new WorkersRealtimeClient(redis.Connection);
        }

        /// <summary>
        /// Create a task instance for a specific task
        /// </summary>
        /// <param name="taskId"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public Task<TaskInstance> AssignAsync(AutomationTask task, string? settings)
        {
            if (task.Package == null)
                throw new ArgumentNullException(nameof(task));

            TaskInstance taskInstance = new TaskInstance(task.Id, task.Package, new InstanceContext() { Settings = settings });
            return AssignAsync(taskInstance);
        }

        /// <summary>
        /// Reassign a task to another worker (for exemple if the current worker crashed). The task state will be passed
        /// to failed.
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        public async Task<TaskInstance> ReassignAsync(TaskInstance task)
        {
            task.State = EnumTaskState.Failed;
            await _repository.UpdateAsync(task.Id, task);
            return await AssignAsync(task);
        }

        private async Task<TaskInstance> AssignAsync(TaskInstance taskInstance)
        {
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
    }
}
