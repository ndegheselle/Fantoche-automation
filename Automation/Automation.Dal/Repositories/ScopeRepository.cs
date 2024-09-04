using Automation.Dal.Models;
using Automation.Shared;
using Automation.Shared.Data;
using MongoDB.Driver;

namespace Automation.Dal.Repositories
{
    public class ScopeRepository : BaseCrudRepository<Scope>
    {
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
                ..(await taskChildrenTask).Select(x => new ScopedElement() {Id = x.Id, Name = x.Name, Type = EnumScopedType.Task})
            ];
            return scope;
        }

        public async Task<Scope> GetRootAsync()
        {
            // Root scope have a fixed Id
            const string rootIdString = "00000000-0000-0000-0000-000000000001";
            var rootId = new Guid(rootIdString);
            var rootScope = await GetByIdAsync(rootId);

            if (rootScope == null)
                throw new Exception("Root scope doesn't exist.");

            return rootScope;
        }
    }
}
