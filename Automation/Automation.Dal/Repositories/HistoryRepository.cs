using Automation.Dal.Models;
using Automation.Shared.Base;
using MongoDB.Driver;

namespace Automation.Dal.Repositories
{
    public class HistoryRepository : BaseRepository<TaskHistory>
    {
        public HistoryRepository(IMongoDatabase database) : base(database, "task_histories")
        {
        }

        public async Task<ListPageWrapper<TaskHistory>> GetByScopeAsync(Guid scopeId, int page, int pageSize)
        {
            var histories = await _collection.Find(e => e.ScopeId == scopeId)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            ListPageWrapper<TaskHistory> pageWrapper = new ListPageWrapper<TaskHistory>(
                page,
                pageSize,
                _collection.CountDocuments(x => x.ScopeId == scopeId));
            return pageWrapper;
        }

        public async Task<ListPageWrapper<TaskHistory>> GetByTaskAsync(Guid taskId, int page, int pageSize)
        {
            var histories = await _collection.Find(e => e.TaskId == taskId)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            ListPageWrapper<TaskHistory> pageWrapper = new ListPageWrapper<TaskHistory>(
            page,
            pageSize,
                _collection.CountDocuments(x => x.TaskId == taskId));
            return pageWrapper;
        }
    }
}
