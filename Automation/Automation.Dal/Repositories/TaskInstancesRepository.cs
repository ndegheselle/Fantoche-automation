using Automation.Models;
using Automation.Models.Work;
using Automation.Shared.Base;
using Automation.Shared.Data.Task;
using MongoDB.Driver;

namespace Automation.Dal.Repositories
{
    public sealed class TaskInstancesRepository : BaseCrudRepository<TaskInstance>
    {
        public TaskInstancesRepository(DatabaseConnection connection) : base(connection, "instances")
        {
        }

        /// <summary>
        /// Get the list of task instances that are not handled by the given workers.
        /// </summary>
        /// <param name="activeWorkersId">Ids of the active workers</param>
        /// <returns>List of task instance than are ynhandled</returns>
        public async Task<IEnumerable<TaskInstance>> GetUnhandledAsync(IEnumerable<string> activeWorkersId)
        {
            var filter = Builders<TaskInstance>.Filter
                .And(
                    Builders<TaskInstance>.Filter.Nin(x => x.WorkerId, activeWorkersId),
                    // XXX : reassign task in progress ? Atomicity of the task data (database, file, ...) ?
                    Builders<TaskInstance>.Filter.In(x => x.State, [EnumTaskState.Pending, EnumTaskState.Waiting, EnumTaskState.Progressing]));

            return await _collection.Find(filter).ToListAsync();
        }

        public async Task<ListPageWrapper<TaskInstance>> GetByTaskAsync(Guid taskId, int page, int pageSize)
        {
            var instances = await _collection.Find(e => e.TaskId == taskId)
                .Skip(page * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            return new ListPageWrapper<TaskInstance>()
            {
                Data = instances,
                Page = page,
                PageSize = pageSize,
                Total = await _collection.CountDocumentsAsync(x => x.TaskId == taskId)
            };
        }

        /// <summary>
        /// Get instances of task, whatever how deep they are in the scope.
        /// </summary>
        /// <param name="scopeId"></param>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public async Task<ListPageWrapper<TaskInstance>> GetByScopeAsync(Guid scopeId, int page, int pageSize)
        {
            TasksRepository taskRepo = new TasksRepository(_connection);
            var tasks = await taskRepo.GetByAnyParentScopeAsync(scopeId);

            var filter = Builders<TaskInstance>.Filter.In(x => x.TaskId, tasks.Select(x => x.Id));
            // We don't load context and result since it may be quite expensive
            var instances = await _collection.Find(filter)
                .Skip(page * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            return new ListPageWrapper<TaskInstance>()
            {
                Data = instances,
                Page = page,
                PageSize = pageSize,
                Total = await _collection.CountDocumentsAsync(filter)
            };
        }
    }
}
