using Automation.Dal.Models;
using Automation.Plugins.Shared;
using Automation.Shared.Base;
using MongoDB.Driver;

namespace Automation.Dal.Repositories
{
    public class TaskIntancesRepository : BaseCrudRepository<TaskInstance>
    {
        public TaskIntancesRepository(IMongoDatabase database) : base(database, "instances")
        {
        }

        /// <summary>
        /// Get the list of task instances that are not handled by the given workers.
        /// </summary>
        /// <param name="activeWorkersId">Ids of the active workers</param>
        /// <returns>List of task instance than are ynhandled</returns>
        public virtual async Task<IEnumerable<TaskInstance>> GetUnhandledAsync(IEnumerable<string> activeWorkersId)
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
            // We don't load context and result since it may be quite extensive
            var projection = Builders<TaskInstance>.Projection.Exclude(s => s.Parameters);
            var instances = await _collection.Find(e => e.TaskId == taskId)
                .Project<TaskInstance>(projection)
                .Skip(page * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            return new ListPageWrapper<TaskInstance>()
            {
                Data = instances,
                Page = page,
                PageSize = pageSize,
                Total = _collection.CountDocuments(x => x.TaskId == taskId)
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
            TasksRepository taskRepo = new TasksRepository(_database);
            var tasks = await taskRepo.GetByAnyParentScopeAsync(scopeId);
            var test = tasks.ToList();

            var filter = Builders<TaskInstance>.Filter.In(x => x.TaskId, tasks.Select(x => x.Id));
            // We don't load context and result since it may be quite expensive
            var projection = Builders<TaskInstance>.Projection.Exclude(s => s.Parameters);
            var instances = await _collection.Find(filter)
                .Project<TaskInstance>(projection)
                .Skip(page * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            return new ListPageWrapper<TaskInstance>()
            {
                Data = instances,
                Page = page,
                PageSize = pageSize,
                Total = _collection.CountDocuments(filter)
            };
        }
    }
}
