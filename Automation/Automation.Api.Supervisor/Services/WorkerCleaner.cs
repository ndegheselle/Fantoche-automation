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
    /// <summary>
    /// Clean the dead workers once in a while, ensure that a task is never hanged if a server crashed.
    /// </summary>
    public class WorkerCleaner : BackgroundService
    {
        private readonly TimeSpan _cleaningInterval = TimeSpan.FromSeconds(30);
        private readonly TaskIntanceRepository _repository;
        private readonly WorkerRealtimeClient _workersClient;
        private readonly WorkerAssignator _assignator;

        public WorkerCleaner(IMongoDatabase database, RedisConnectionManager redis)
        {
            _repository = new TaskIntanceRepository(database);
            _workersClient = new WorkerRealtimeClient(redis);
            _assignator = new WorkerAssignator(database, redis);
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await CleanDeadWorkers();
                await Task.Delay(_cleaningInterval, stoppingToken);
            }
        }

        private async Task CleanDeadWorkers()
        {
            IEnumerable<string> workers = await _workersClient.GetDeadWorkersIdsAsync();

            // TODO : should change the state of the worker instead so that it doesn't dissapear from supervisor
            foreach (string deadWorkerId in workers)
            {
                await _workersClient.RemoveWorkerAsync(deadWorkerId);
            }

            // Assign the dead workers tasks that are not finished to some other workers
            foreach (string deadWorkerId in workers)
            {
                // XXX : reassign task in progress ? Atomicity of the task data (database, file, ...) ?
                IEnumerable<TaskInstance>? tasks = await _repository.GetByWorkerAndStateAsync(
                    deadWorkerId,
                    [EnumTaskState.Pending, EnumTaskState.Progress]);

                foreach (TaskInstance task in tasks)
                    await _assignator.ReassignAsync(task);
            }
        }
    }
}
