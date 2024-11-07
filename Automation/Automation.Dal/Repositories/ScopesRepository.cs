using Automation.Dal.Models;
using MongoDB.Driver;

namespace Automation.Dal.Repositories
{
    public class ScopesRepository : BaseCrudRepository<Scope>
    {
        // Root scope have a fixed Id
        public static readonly Guid ROOT_SCOPE_ID = new Guid("00000000-0000-0000-0000-000000000001");

        public ScopesRepository(IMongoDatabase database) : base(database, "scopes")
        {
        }

        /// <summary>
        /// Delete all tasks and scopes child of a scope, whatever how deep they are.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public override async Task DeleteAsync(Guid id)
        {
            TasksRepository taskRepository = new TasksRepository(_database);
            await taskRepository.DeleteByScopeAsync(id);
            await DeleteByScopeAsync(id);
            await _collection.DeleteOneAsync(e => e.Id == id);
        }

        /// <summary>
        /// Delete all scopes child of a scope, whatever how deep they are.
        /// </summary>
        /// <param name="scopeId"></param>
        /// <returns></returns>
        private async Task DeleteByScopeAsync(Guid scopeId)
        {
            var filter = Builders<Scope>.Filter.AnyEq(x => x.ParentTree, scopeId);
            await _collection.DeleteManyAsync(filter);
        }

        public async Task<IEnumerable<Scope>> GetByScopeAsync(Guid scopeId)
        {
            var projection = Builders<Scope>.Projection.Include(s => s.Id).Include(s => s.Name);
            return await _collection.Find(e => e.ParentId == scopeId).Project<Scope>(projection).ToListAsync();
        }

        public override Task<Scope?> GetByIdAsync(Guid id) { return GetByIdAsync(id, true); }

        public async Task<Scope?> GetByIdAsync(Guid id, bool withChildrens)
        {
            var scope = await _collection.Find(e => e.Id == id).FirstOrDefaultAsync();

            if (scope == null)
                return null;

            if (withChildrens)
            {
                var taskRepo = new TasksRepository(_database);

                var scopeChildrenTask = GetByScopeAsync(scope.Id);
                var taskChildrenTask = taskRepo.GetByParentScopeAsync(scope.Id);

                await Task.WhenAll(scopeChildrenTask, taskChildrenTask);

                scope.Childrens = [
                    ..await scopeChildrenTask,
                ..await taskChildrenTask
                ];
            }

            return scope;
        }

        public async Task<Scope> GetRootAsync()
        {
            var rootScope = await GetByIdAsync(ROOT_SCOPE_ID);

            if (rootScope == null)
                throw new Exception("Root scope doesn't exist.");

            return rootScope;
        }

        public async Task<ScopedElement?> GetDirectChildByNameAsync(Guid? scopeId, string name)
        {
            var scope = await _collection.Find(e => e.Name == name && e.ParentId == scopeId).FirstOrDefaultAsync();
            if (scope != null)
                return scope;

            var taskRepo = new TasksRepository(_database);
            var task = await taskRepo.GetByParentScopeAndNameAsync(scopeId ?? ROOT_SCOPE_ID, name);
            if (task != null)
                return task;
            return null;
        }
    }
}
