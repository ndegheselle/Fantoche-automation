using Automation.Dal.Models;
using Automation.Shared;
using MongoDB.Driver;

namespace Automation.Dal.Repositories
{
    public class TaskRepository : BaseCrudRepository<TaskNode>
    {
        public TaskRepository(IMongoDatabase database) : base(database, "tasks")
        {}

        public async Task<IEnumerable<TaskNode>> GetByScopeAsync(Guid scopeId)
        {
            var projection = Builders<TaskNode>.Projection.Include(s => s.Id).Include(s => s.Name);
            return await _collection.Find(e => e.ScopeId == scopeId).Project<TaskNode>(projection).ToListAsync();
        }

        public async Task<TaskNode?> GetByScopeAndNameAsync(Guid scopeId, string name)
        {
            var projection = Builders<TaskNode>.Projection.Include(s => s.Id).Include(s => s.Name);
            return await _collection.Find(e => e.ScopeId == scopeId && e.Name == name).Project<TaskNode>(projection).FirstOrDefaultAsync();
        }

        public async Task DeletebyScopeAsync(Guid scopeId)
        {
            await _collection.DeleteManyAsync(e => e.ScopeId == scopeId);
        }
    }
}
