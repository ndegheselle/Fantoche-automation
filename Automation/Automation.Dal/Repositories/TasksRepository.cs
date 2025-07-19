using Automation.Dal.Models;
using Automation.Shared.Data;
using MongoDB.Driver;

namespace Automation.Dal.Repositories
{
    public class TasksRepository : BaseCrudRepository<BaseAutomationTask>
    {
        public TasksRepository(IMongoDatabase database) : base(database, "tasks")
        {
        }

        public async Task<IEnumerable<BaseAutomationTask>> GetByAnyParentScopeAsync(Guid scopeId)
        {
            // Include the discriminator field so that derived types are kept
            var projection = Builders<BaseAutomationTask>.Projection.Include(s => s.Id).Include(s => s.Metadata).Include("_t");
            var filter = Builders<BaseAutomationTask>.Filter.AnyEq(x => x.ParentTree, scopeId);
            return await _collection.Find(filter).Project<BaseAutomationTask>(projection).ToListAsync();
        }

        public async Task<IEnumerable<BaseAutomationTask>> GetByDirectParentScopeAsync(Guid scopeId)
        {
            var projection = Builders<BaseAutomationTask>.Projection.Include(s => s.Id).Include(s => s.Metadata).Include("_t");
            return await _collection.Find(e => e.ParentId == scopeId).Project<BaseAutomationTask>(projection).ToListAsync();
        }

        public async Task<BaseAutomationTask?> GetByParentScopeAndNameAsync(Guid scopeId, string name)
        {
            var projection = Builders<BaseAutomationTask>.Projection.Include(s => s.Id).Include(s => s.Metadata).Include("_t");
            return await _collection.Find(e => e.ParentId == scopeId && e.Metadata.Name == name).Project<BaseAutomationTask>(projection).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Delete all tasks related child of a scope, whatever how deep they are.
        /// </summary>
        /// <param name="scopeId"></param>
        /// <returns></returns>
        public async Task DeleteByScopeAsync(Guid scopeId)
        {
            var filter = Builders<BaseAutomationTask>.Filter.AnyEq(x => x.ParentTree, scopeId);
            await _collection.DeleteManyAsync(filter); 
        }

        public async Task<IEnumerable<TaskSchedule>> GetScheduled()
        {
            var projection = Builders<BaseAutomationTask>.Projection.Include(s => s.Id).Include(s => s.Schedules).Include("_t");

            var filter = Builders<BaseAutomationTask>.Filter
               .And(
                   Builders<BaseAutomationTask>.Filter.Ne(x => x.Schedules, null),
                   Builders<BaseAutomationTask>.Filter.Not(Builders<BaseAutomationTask>.Filter.Size(nameof(BaseAutomationTask.Schedules), 0)));

            var scheduledTasks = await _collection.Find(filter).Project<BaseAutomationTask>(projection).ToListAsync();
            return scheduledTasks.SelectMany(t => t.Schedules.Select(s => new TaskSchedule(t.Id, s)));
        }
    }
}
