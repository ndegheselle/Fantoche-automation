using Automation.Dal.Models;
using Automation.Shared;
using Automation.Shared.Contracts;
using MongoDB.Driver;

namespace Automation.Dal.Repositories
{
    public class ScopeRepository : BaseCrudRepository<Scope>, IScopeRepository<Scope>
    {
        public ScopeRepository(IMongoDatabase database) : base(database, "Scopes")
        {}

        public async Task<IEnumerable<Scope>> GetByScopeAsync(Guid scopeId)
        {
            var projection = Builders<Scope>.Projection.Include(s => s.Id).Include(s => s.Name);
            return await _collection.Find(e => e.ParentId == scopeId).Project<Scope>(projection).ToListAsync();
        }

        public async override Task<Scope?> GetByIdAsync(Guid id)
        {
            var scope = await _collection.Find(e => e.Id == id).FirstAsync();

            var taskRepo = new TaskRepository(_database);
            var workflowRepo = new WorkflowRepository(_database);

            var scopeChildrenTask = GetByScopeAsync(scope.Id);
            var workflowChildrenTask = workflowRepo.GetByScopeAsync(scope.Id);
            var taskChildrenTask = taskRepo.GetByScopeAsync(scope.Id);

            await Task.WhenAll(scopeChildrenTask, workflowChildrenTask, taskChildrenTask);

            scope.Childrens = [
                ..await scopeChildrenTask,
                ..await workflowChildrenTask,
                ..await taskChildrenTask
            ];
            return scope;
        }

        public async Task<Scope> GetRootAsync()
        {
            const string rootIdString = "00000000-0000-0000-0000-000000000001";
            var rootId = new Guid(rootIdString);
            var rootScope = await GetByIdAsync(rootId);

            if (rootScope == null)
                throw new Exception("Root scope doesn't exist.");

            return rootScope;
        }
    }
}
