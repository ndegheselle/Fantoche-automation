using Automation.Dal.Models;
using Automation.Dal.Repositories;
using Automation.Realtime;
using Automation.Realtime.Clients;
using Automation.Realtime.Models;
using Automation.Shared.Data;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Automation.Api.Supervisor.Business
{

    public class WorkerAssignator
    {
        private readonly TaskIntanceRepository _repository;
        private readonly WorkerRealtimeClient _workersClient;
        private readonly TasksRealtimeClient _tasksClient;

        public WorkerAssignator(IMongoDatabase database, RedisConnectionManager redis)
        {
            _repository = new TaskIntanceRepository(database);
            _workersClient = new WorkerRealtimeClient(redis);
            _tasksClient = new TasksRealtimeClient(redis);
        }

        /// <summary>
        /// Create a task instance for a specific task
        /// </summary>
        /// <param name="taskId"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public async Task<TaskInstance> AssignAsync(Guid taskId, BsonDocument? parameters)
        {
            TaskInstance task = new TaskInstance() { Parameters = parameters, TaskId = taskId, };
            WorkerInstance selectedWorker = await SelectWorkerAsync(task);
            task.WorkerId = selectedWorker.Id;
            await _repository.CreateAsync(task);
            _tasksClient.Notify(selectedWorker.Id, task.Id);
            return task;
        }

        /// <summary>
        /// Reassign a task to another worker (for exemple if the current worker crashed).
        /// The task state will be passed to failed.
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        public async Task<TaskInstance> ReassignAsync(TaskInstance task)
        {
            task.State = EnumTaskState.Failed;
            await _repository.UpdateAsync(task.Id, task);
            return await AssignAsync(task.TaskId, task.Parameters.ToBsonDocument());
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
            return workers.MinBy(x => x.QueueSize) ?? throw new Exception("No available worker for the task.");
        }
    }
}
