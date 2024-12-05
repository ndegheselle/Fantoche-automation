using Automation.Dal.Models;
using Automation.Shared.Data;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Automation.Dal.Repositories
{
    public class TasksRepository : BaseCrudRepository<TaskNode>
    {
        public TasksRepository(IMongoDatabase database) : base(database, "tasks")
        {
        }

        public async Task<IEnumerable<TaskNode>> GetByAnyParentScopeAsync(Guid scopeId)
        {
            var projection = Builders<TaskNode>.Projection.Include(s => s.Id).Include(s => s.Name);
            var filter = Builders<TaskNode>.Filter.AnyEq(x => x.ParentTree, scopeId);
            return await _collection.Find(filter).Project<TaskNode>(projection).ToListAsync();
        }

        public async Task<IEnumerable<TaskNode>> GetByParentScopeAsync(Guid scopeId)
        {
            var projection = Builders<TaskNode>.Projection.Include(s => s.Id).Include(s => s.Name);
            return await _collection.Find(e => e.ParentId == scopeId).Project<TaskNode>(projection).ToListAsync();
        }

        public async Task<TaskNode?> GetByParentScopeAndNameAsync(Guid scopeId, string name)
        {
            var projection = Builders<TaskNode>.Projection.Include(s => s.Id).Include(s => s.Name);
            return await _collection.Find(e => e.ParentId == scopeId && e.Name == name).Project<TaskNode>(projection).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Delete all tasks related child of a scope, whatever how deep they are.
        /// </summary>
        /// <param name="scopeId"></param>
        /// <returns></returns>
        public async Task DeleteByScopeAsync(Guid scopeId)
        {
            var filter = Builders<TaskNode>.Filter.AnyEq(x => x.ParentTree, scopeId);
            await _collection.DeleteManyAsync(filter); 
        }

        public async Task<IEnumerable<TaskSchedule>> GetScheduled()
        {
            var projection = Builders<TaskNode>.Projection.Include(s => s.Id).Include(s => s.Schedules);

            var filter = Builders<TaskNode>.Filter
               .And(
                   Builders<TaskNode>.Filter.Ne(x => x.Schedules, null),
                   Builders<TaskNode>.Filter.Not(Builders<TaskNode>.Filter.Size(nameof(TaskNode.Schedules), 0)));

            var scheduledTasks = await _collection.Find(filter).Project<TaskNode>(projection).ToListAsync();
            return scheduledTasks.SelectMany(t => t.Schedules.Select(s => new TaskSchedule(t.Id, s)));
        }
    }
}
