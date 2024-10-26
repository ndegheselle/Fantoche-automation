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
        /// Assign a worker to an existing task (for exemple if the current worker crashed)
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        public async Task<TaskInstance> AssignAsync(TaskInstance task)
        {
            WorkerInstance selectedWorker = await SelectWorkerAsync(task);
            task.WorkerId = selectedWorker.Id;
            await _repository.UpdateAsync(task.Id, task);
            _tasksClient.Notify(selectedWorker.Id, task.Id);
            return task;
        }

        private async Task<WorkerInstance> SelectWorkerAsync(TaskInstance task)
        {
            IEnumerable<WorkerInstance> workers = await _workersClient.GetWorkersAsync();
            // TODO : load balancing and select a worker based on tasks params
            return workers.First();
        }
    }
}
