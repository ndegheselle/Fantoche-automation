using Automation.Dal.Models;
using Automation.Shared.Base;
using MongoDB.Driver;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

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
                .Skip(page * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            return new ListPageWrapper<TaskHistory>()
            {
                Data = histories,
                Page = page,
                PageSize = pageSize,
                Total = _collection.CountDocuments(x => x.ScopeId == scopeId)
            };
        }

        public async Task<ListPageWrapper<TaskHistory>> GetByTaskAsync(Guid taskId, int page, int pageSize)
        {
            var histories = await _collection.Find(e => e.TaskId == taskId)
                .Skip(page * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            return new ListPageWrapper<TaskHistory>()
            {
                Data = histories,
                Page = page,
                PageSize = pageSize,
                Total = _collection.CountDocuments(x => x.TaskId == taskId)
            };
        }
    }
}
