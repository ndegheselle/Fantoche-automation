using Automation.Dal.Models;
using Automation.Shared;
using Automation.Shared.Base;
using MongoDB.Driver;
using static System.Formats.Asn1.AsnWriter;

namespace Automation.Dal.Repositories
{
    public class HistoryRepository : BaseRepository<TaskHistory>, IHistoryClient<TaskHistory>
    {
        public HistoryRepository(IMongoDatabase database) : base(database, "TaskHistories")
        {
        }

        public async Task<IPageWrapper<TaskHistory>> GetByScopeAsync(Guid scopeId, int page, int pageSize)
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

        public async Task<IPageWrapper<TaskHistory>> GetByTaskAsync(Guid taskId, int page, int pageSize)
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
