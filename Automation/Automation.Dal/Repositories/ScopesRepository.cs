using Automation.Models.Work;
using MongoDB.Driver;

namespace Automation.Dal.Repositories
{
    public class ScopesRepository : BaseCrudRepository<Scope>
    {
        public ScopesRepository(DatabaseConnection connection) : base(connection, "scopes")
        {}

        /// <summary>
        /// Delete all tasks and scopes child of a scope, whatever how deep they are.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public override async Task DeleteAsync(Guid id)
        {
            TasksRepository taskRepository = new TasksRepository(_connection);
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

        public async Task<IEnumerable<Scope>> GetByScopeAsync(Guid scopeId, bool withChildrens = true)
        {
            var projection = Builders<Scope>.Projection.Include(s => s.Id).Include(s => s.Metadata);
            var scopes = await _collection.Find(e => e.ParentId == scopeId).Project<Scope>(projection).ToListAsync();

            if (!withChildrens)
                return scopes;

            // Load childrens of the scopes
            var taskRepo = new TasksRepository(_connection);
            foreach (var scope in scopes)
            {
                var scopeChildrenTask = GetByScopeAsync(scope.Id, false);
                var taskChildrenTask = taskRepo.GetByDirectParentScopeAsync(scope.Id);

                await Task.WhenAll(scopeChildrenTask, taskChildrenTask);

                scope.Childrens = [
                    ..await scopeChildrenTask,
                    ..await taskChildrenTask
                ];
            }

            return scopes;
        }

        public override Task<Scope> GetByIdAsync(Guid id) { return GetByIdAsync(id, true); }

        public async Task<Scope> GetByIdAsync(Guid id, bool withChildrens)
        {
            var scope = await _collection.Find(e => e.Id == id).FirstOrDefaultAsync();

            if (scope == null)
                throw new Exception($"Unknow element with id '{id}'");

            if (withChildrens)
            {
                var taskRepo = new TasksRepository(_connection);

                var scopeChildrenTask = GetByScopeAsync(scope.Id, true);
                var taskChildrenTask = taskRepo.GetByDirectParentScopeAsync(scope.Id);

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
            var rootScope = await GetByIdAsync(Scope.ROOT_SCOPE_ID);

            if (rootScope == null)
                throw new Exception("Root scope doesn't exist.");

            return rootScope;
        }

        /// <summary>
        /// Get a task by its parent scope and the task name name.
        /// </summary>
        /// <param name="scopeId"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task<bool> IsNameUsedAsync(Guid scopeId, string name)
        {
            return await _collection.Find(e => e.ParentId == scopeId && e.Metadata.Name == name).AnyAsync();
        }
    }
}
