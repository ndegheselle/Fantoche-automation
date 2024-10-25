using Automation.Dal.Models;
using Automation.Dal.Repositories;
using Automation.Realtime;
using Automation.Realtime.Clients;
using Automation.Realtime.Models;
using MongoDB.Bson;
using MongoDB.Driver;

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

        public async Task<TaskInstance> AssignToWorkerAsync(Guid taskId, BsonDocument? parameters)
        {
            TaskInstance task = new TaskInstance()
            {
                Parameters = parameters,
                TaskId = taskId,
            };

            WorkerInstance selectedWorker = await SelectWorkerAsync(task);
            task.WorkerId = selectedWorker.Id;

            await _repository.CreateAsync(task);
            _tasksClient.Notify(selectedWorker.Id, task.Id);
            return task;
        }

        private async Task<WorkerInstance> SelectWorkerAsync(TaskInstance task)
        {
            IEnumerable<WorkerInstance> workers = await _workersClient.GetWorkersAsync();

            foreach (WorkerInstance worker in workers)
            {
                if (await _workersClient)
            }

            // TODO : load balancing and select a worker based on tasks params
            return workers.First();
        }
    }
}
