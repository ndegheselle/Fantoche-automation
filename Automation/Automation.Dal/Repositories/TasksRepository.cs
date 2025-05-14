using Automation.Dal.Models;
using Automation.Shared.Data;
using MongoDB.Driver;

namespace Automation.Dal.Repositories
{
    public class TasksRepository : BaseCrudRepository<AutomationTask>
    {
        public TasksRepository(IMongoDatabase database) : base(database, "tasks")
        {
        }

        public async Task<IEnumerable<AutomationTask>> GetByAnyParentScopeAsync(Guid scopeId)
        {
            // Include the discriminator field so that derived types are kept
            var projection = Builders<AutomationTask>.Projection.Include(s => s.Id).Include(s => s.Metadata).Include("_t");
            var filter = Builders<AutomationTask>.Filter.AnyEq(x => x.ParentTree, scopeId);
            return await _collection.Find(filter).Project<AutomationTask>(projection).ToListAsync();
        }

        public async Task<IEnumerable<AutomationTask>> GetByDirectParentScopeAsync(Guid scopeId)
        {
            var projection = Builders<AutomationTask>.Projection.Include(s => s.Id).Include(s => s.Metadata).Include("_t");
            return await _collection.Find(e => e.ParentId == scopeId).Project<AutomationTask>(projection).ToListAsync();
        }

        public async Task<AutomationTask?> GetByParentScopeAndNameAsync(Guid scopeId, string name)
        {
            var projection = Builders<AutomationTask>.Projection.Include(s => s.Id).Include(s => s.Metadata).Include("_t");
            return await _collection.Find(e => e.ParentId == scopeId && e.Metadata.Name == name).Project<AutomationTask>(projection).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Delete all tasks related child of a scope, whatever how deep they are.
        /// </summary>
        /// <param name="scopeId"></param>
        /// <returns></returns>
        public async Task DeleteByScopeAsync(Guid scopeId)
        {
            var filter = Builders<AutomationTask>.Filter.AnyEq(x => x.ParentTree, scopeId);
            await _collection.DeleteManyAsync(filter); 
        }

        public async Task<IEnumerable<TaskSchedule>> GetScheduled()
        {
            var projection = Builders<AutomationTask>.Projection.Include(s => s.Id).Include(s => s.Schedules).Include("_t");

            var filter = Builders<AutomationTask>.Filter
               .And(
                   Builders<AutomationTask>.Filter.Ne(x => x.Schedules, null),
                   Builders<AutomationTask>.Filter.Not(Builders<AutomationTask>.Filter.Size(nameof(AutomationTask.Schedules), 0)));

            var scheduledTasks = await _collection.Find(filter).Project<AutomationTask>(projection).ToListAsync();
            return scheduledTasks.SelectMany(t => t.Schedules.Select(s => new TaskSchedule(t.Id, s)));
        }
    }
}
