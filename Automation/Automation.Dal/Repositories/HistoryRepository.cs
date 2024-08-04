using Automation.Dal.Models;
using Automation.Shared;
using Automation.Shared.Base;
using MongoDB.Driver;

namespace Automation.Dal.Repositories
{
    public class HistoryRepository : BaseRepository<TaskHistory>, IHistoryRepository<TaskHistory>
    {
        public HistoryRepository(IMongoDatabase database) : base(database, "TaskHistories")
        {}

        public async Task<PageWrapper<TaskHistory>> GetByScopeAsync(Guid scopeId, int page, int pageSize)
        {
            var histories = await _collection.Find(e => e.ScopeId == scopeId).Skip((page - 1) * pageSize).Limit(pageSize).ToListAsync();

            PageWrapper<TaskHistory> pageWrapper = new PageWrapper<TaskHistory>
            {
                Data = histories,
                Page = page,
                PageSize = pageSize,
                Total = _collection.CountDocuments(x => x.ScopeId == scopeId)
            };

            return pageWrapper;
        }

        public async Task<PageWrapper<TaskHistory>> GetByTaskAsync(Guid taskId, int page, int pageSize)
        {
            var histories = await _collection.Find(e => e.TaskId == taskId).Skip((page - 1) * pageSize).Limit(pageSize).ToListAsync();

            PageWrapper<TaskHistory> pageWrapper = new PageWrapper<TaskHistory>
            {
                Data = histories,
                Page = page,
                PageSize = pageSize,
                Total = _collection.CountDocuments(x => x.TaskId == taskId)
            };

            return pageWrapper;
        }
    }
}
