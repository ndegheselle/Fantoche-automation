using Automation.Dal.Models;
using Automation.Dal.Repositories;
using Automation.Shared.Data;

namespace Automation.Dal
{
    /// <summary>
    /// Set the default values that should be present in the database
    /// </summary>
    public class DatabaseSeeder
    {
        private readonly ScopesRepository _scopeRepo;
        private readonly TasksRepository _tasksRepo;

        public DatabaseSeeder(DatabaseConnection connection)
        {
            _scopeRepo = new ScopesRepository(connection);
            _tasksRepo = new TasksRepository(connection);
        }

        public async Task Seed()
        {
            await _scopeRepo.CreateOrUpdateAsync(new Scope()
            {
                Id = Scope.ROOT_SCOPE_ID,
                Metadata = new ScopedMetadata(EnumScopedType.Scope)
                {
                    Name = "..",
                },
            });

            Guid controlsScopeId = await _scopeRepo.CreateOrUpdateAsync(new Scope()
            {
                Id = ScopeControlId,
                ParentId = Scope.ROOT_SCOPE_ID,
                ParentTree = [Scope.ROOT_SCOPE_ID],
                Metadata = new ScopedMetadata(EnumScopedType.Scope)
                {
                    Name = "Controls",
                    IsReadOnly = true,
                },
            });

            // Control tasks
            await _tasksRepo.CreateOrUpdateAsync(new AutomationTask()
            {
                Id = StartTask.Id,
                ParentId = controlsScopeId,
                ParentTree = [Scope.ROOT_SCOPE_ID, controlsScopeId],
                Metadata = new ScopedMetadata(EnumScopedType.Task)
                {
                    Name = "Start",
                    Icon = "\uf04b",
                    IsReadOnly = true
                },
                Target = new ClassTarget(StartTask.Identifier)
            });
        }
    }
}
