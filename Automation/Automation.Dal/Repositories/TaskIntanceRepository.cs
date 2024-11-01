using Automation.Dal.Models;
using Automation.Plugins.Shared;
using Automation.Shared.Base;
using Automation.Shared.Data;
using MongoDB.Driver;

namespace Automation.Dal.Repositories
{
    public class TaskIntanceRepository : BaseCrudRepository<TaskInstance>
    {
        public TaskIntanceRepository(IMongoDatabase database) : base(database, "task_instances")
        {
        }

        public virtual async Task<IEnumerable<TaskInstance>> GetUnhandledAsync(IEnumerable<string> activeWorkersId)
        {
            var filter = Builders<TaskInstance>.Filter
                .And(
                    Builders<TaskInstance>.Filter.Nin(x => x.WorkerId, activeWorkersId),
                    // XXX : reassign task in progress ? Atomicity of the task data (database, file, ...) ?
                    Builders<TaskInstance>.Filter.In(x => x.State, [EnumTaskState.Pending, EnumTaskState.Progress]));

            return await _collection.Find(filter).ToListAsync();
        }


        public async Task<ListPageWrapper<TaskInstance>> GetByScopeAsync(Guid scopeId, int page, int pageSize)
        {
            var histories = await _collection.Find(e => e.ScopeId == scopeId)
                .Skip(page * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            return new ListPageWrapper<TaskInstance>()
            {
                Data = histories,
                Page = page,
                PageSize = pageSize,
                Total = _collection.CountDocuments(x => x.ScopeId == scopeId)
            };
        }

        public async Task<ListPageWrapper<TaskInstance>> GetByTaskAsync(Guid taskId, int page, int pageSize)
        {
            var histories = await _collection.Find(e => e.TaskId == taskId)
                .Skip(page * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            return new ListPageWrapper<TaskInstance>()
            {
                Data = histories,
                Page = page,
                PageSize = pageSize,
                Total = _collection.CountDocuments(x => x.TaskId == taskId)
            };
        }
    }
}
