using Automation.Models;
using Automation.Models.Work;
using Automation.Shared.Data;
using MongoDB.Driver;

namespace Automation.Dal.Repositories
{
    public class TasksRepository : BaseCrudRepository<BaseAutomationTask>
    {
        public TasksRepository(DatabaseConnection connection) : base(connection, "tasks")
        {
        }

        public override async Task<List<BaseAutomationTask>> GetAllAsync()
        {
            // Include the discriminator field so that derived types are kept
            var projection = Builders<BaseAutomationTask>.Projection
                .Include(s => s.Id)
                .Include(s => s.Metadata)
                .Include(s => s.InputSchemaJson)
                .Include(s => s.OutputSchemaJson)
                .Include("_t");
            return await _collection.Find(_ => true).Project<BaseAutomationTask>(projection).ToListAsync();
        }

        /// <summary>
        /// Get all tasks that are children of a scope, whatever how deep they are.
        /// </summary>
        /// <param name="scopeId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<BaseAutomationTask>> GetByAnyParentScopeAsync(Guid scopeId)
        {
            // Include the discriminator field so that derived types are kept
            var projection = Builders<BaseAutomationTask>.Projection
                .Include(s => s.Id)
                .Include(s => s.Metadata)
                .Include("_t");
            var filter = Builders<BaseAutomationTask>.Filter.AnyEq(x => x.ParentTree, scopeId);
            return await _collection.Find(filter).Project<BaseAutomationTask>(projection).ToListAsync();
        }

        /// <summary>
        /// Get all tasks that are direct children of a scope.
        /// </summary>
        /// <param name="scopeId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<BaseAutomationTask>> GetByDirectParentScopeAsync(Guid scopeId)
        {
            var projection = Builders<BaseAutomationTask>.Projection
                .Include(s => s.Id)
                .Include(s => s.Metadata)
                .Include("_t");
            return await _collection.Find(e => e.ParentId == scopeId)
                .Project<BaseAutomationTask>(projection)
                .ToListAsync();
        }

        /// <summary>
        /// Get all tasks by tag.
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public async Task<IEnumerable<BaseAutomationTask>> GetByTagAsync(string tag)
        {
            var projection = Builders<BaseAutomationTask>.Projection
                .Include(s => s.Id)
                .Include(s => s.Metadata)
                .Include("_t");
            return await _collection.Find(e => e.Metadata.Tags.Contains(tag))
                .Project<BaseAutomationTask>(projection)
                .ToListAsync();
        }

        /// <summary>
        /// Get a task by its parent scope and the task name name.
        /// </summary>
        /// <param name="scopeId"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task<bool> IsNameUsedAsync(Guid scopeId, string name)
        { return await _collection.Find(e => e.ParentId == scopeId && e.Metadata.Name == name).AnyAsync(); }

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

        public async Task<IEnumerable<TaskSchedule>> GetScheduledAsync()
        {
            var projection = Builders<BaseAutomationTask>.Projection
                .Include(s => s.Id)
                .Include(s => s.Schedules)
                .Include("_t");

            var filter = Builders<BaseAutomationTask>.Filter
                .And(
                    Builders<BaseAutomationTask>.Filter.Ne(x => x.Schedules, null),
                    Builders<BaseAutomationTask>.Filter
                        .Not(Builders<BaseAutomationTask>.Filter.Size(nameof(BaseAutomationTask.Schedules), 0)));

            var scheduledTasks = await _collection.Find(filter).Project<BaseAutomationTask>(projection).ToListAsync();
            return scheduledTasks.SelectMany(t => t.Schedules.Select(s => new TaskSchedule(t.Id, s)));
        }

        /// <summary>
        /// Get all unique tags from all tasks.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<string>> GetTagsAsync()
        {
            return await _collection
                .Distinct<string>("Metadata.Tags", FilterDefinition<BaseAutomationTask>.Empty)
                .ToListAsync();
        }
    }
}
