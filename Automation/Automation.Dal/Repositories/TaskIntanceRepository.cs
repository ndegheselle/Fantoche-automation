using Automation.Dal.Models;
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

        public async Task<ListPageWrapper<TaskInstance>> GetByTaskAsync(Guid taskId, int page, int pageSize)
        {
            // We don't load context and result since it may be quite extensive
            var projection = Builders<TaskInstance>.Projection
                .Exclude(s => s.Context)
                .Exclude(s => s.Result);
            var histories = await _collection.Find(e => e.TaskId == taskId)
                .Project<TaskInstance>(projection)
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
