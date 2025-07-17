using Automation.Dal.Models;
using Automation.Dal.Repositories;
using Automation.Shared.Data;
using Automation.Worker.Control.Flow;
using MongoDB.Driver;

namespace Automation.Supervisor.Api.Database
{
    /// <summary>
    /// Set the default values that should be present in the database
    /// </summary>
    public class DatabaseSeeder
    {
        private readonly ScopesRepository _scopeRepo;
        private readonly TasksRepository _tasksRepo;

        public DatabaseSeeder(IMongoDatabase database)
        {
            _scopeRepo = new ScopesRepository(database);
            _tasksRepo = new TasksRepository(database);
        }

        public async Task Seed()
        {
            await _scopeRepo.CreateIfDoesntExistAsync(new Scope()
            {
                Id = IScope.ROOT_SCOPE_ID,
                Metadata = new ScopedMetadata(EnumScopedType.Scope)
                {
                    Name = "..",
                },
            });

            Guid controlsScopeId = await _scopeRepo.CreateIfDoesntExistAsync(new Scope()
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                ParentId = IScope.ROOT_SCOPE_ID,
                ParentTree = [IScope.ROOT_SCOPE_ID],
                Metadata = new ScopedMetadata(EnumScopedType.Scope)
                {
                    Name = "Controls",
                    IsReadOnly = true,
                },
            });

            // Control tasks
            await _tasksRepo.CreateIfDoesntExistAsync(new BaseAutomationTask()
            {
                Id = StartTask.Id,
                ParentId = controlsScopeId,
                ParentTree = [IScope.ROOT_SCOPE_ID, controlsScopeId],
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
