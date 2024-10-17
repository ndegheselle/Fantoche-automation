using Automation.Dal.Models;
using Automation.Dal.Repositories;
using Automation.Realtime;
using Automation.Realtime.Clients;
using Automation.Realtime.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Operations;

namespace Automation.Supervisor
{
    public class TaskSuperviser
    {
        private readonly TaskIntanceRepository _repository;
        private readonly WorkerRealtimeClient _workersClient;
        private readonly TasksRealtimeClient _tasksClient;

        public TaskSuperviser(IMongoDatabase database, RedisConnectionManager redis)
        {
            _repository = new TaskIntanceRepository(database);
            _workersClient = new WorkerRealtimeClient(redis);
            _tasksClient = new TasksRealtimeClient(redis);
        }

        public async Task<TaskInstance> AssignToWorkerAsync(Guid taskId, object parameters)
        {
            TaskInstance task = new TaskInstance()
            {
                Parameters = parameters.ToBsonDocument(),
                TaskId = taskId,
            };
            await _repository.CreateAsync(task);
            WorkerInstance selectedWorker = await SelectWorkerAsync(task);
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
