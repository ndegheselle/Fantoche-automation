using Automation.Dal.Models;
using Automation.Shared;
using Automation.Shared.Data;
using MongoDB.Driver;

namespace Automation.Dal.Repositories
{
    public class ScopeRepository : BaseCrudRepository<Scope>
    {
        // Root scope have a fixed Id
        public readonly Guid ROOT_SCOPE_ID = new Guid("00000000-0000-0000-0000-000000000001");

        public ScopeRepository(IMongoDatabase database) : base(database, "scopes")
        {}

        public override Task DeleteAsync(Guid id)
        {
            return DeleteRecursivelyAsync(id);
        }

        private async Task DeleteRecursivelyAsync(Guid id)
        {
            // Remove subscope
            foreach (Scope scope in await GetByScopeAsync(id))
            {
                await DeleteRecursivelyAsync(scope.Id);
            }
            TaskRepository taskRepository = new TaskRepository(_database);
            await taskRepository.DeletebyScopeAsync(id);
            await base.DeleteAsync(id);
        }

        public async Task<IEnumerable<Scope>> GetByScopeAsync(Guid scopeId)
        {
            var projection = Builders<Scope>.Projection.Include(s => s.Id).Include(s => s.Name);
            return await _collection.Find(e => e.ParentId == scopeId).Project<Scope>(projection).ToListAsync();
        }

        public async override Task<Scope?> GetByIdAsync(Guid id)
        {
            var scope = await _collection.Find(e => e.Id == id).FirstOrDefaultAsync();

            if (scope == null)
                return null;

            var taskRepo = new TaskRepository(_database);

            var scopeChildrenTask = GetByScopeAsync(scope.Id);
            var taskChildrenTask = taskRepo.GetByScopeAsync(scope.Id);

            await Task.WhenAll(scopeChildrenTask, taskChildrenTask);

            scope.Childrens = [
                ..await scopeChildrenTask,
                ..(await taskChildrenTask).Select(x => new ScopedElement(x))
            ];
            return scope;
        }

        public async Task<Scope> GetRootAsync()
        {
            var rootScope = await GetByIdAsync(ROOT_SCOPE_ID);

            if (rootScope == null)
                throw new Exception("Root scope doesn't exist.");

            return rootScope;
        }

        public async Task<ScopedElement?> GetChildByNameAsync(Guid? scopeId, string name)
        {
            var scope = await _collection.Find(e => e.Name == name && e.ParentId == scopeId).FirstOrDefaultAsync();
            if (scope != null)
                return scope;

            var taskRepo = new TaskRepository(_database);
            var task = await taskRepo.GetByScopeAndNameAsync(scopeId ?? ROOT_SCOPE_ID, name);
            if (task != null)
                return new ScopedElement(task);
            return null;
        }
    }
}
