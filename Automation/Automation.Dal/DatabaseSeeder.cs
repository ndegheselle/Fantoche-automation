using Automation.Models;
using Automation.Dal.Repositories;
using Automation.Shared.Data;
using Automation.Models.Work;

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

        public async Task Seed(Scope controlScope)
        {
            await _scopeRepo.CreateOrReplaceAsync(new Scope()
            {
                Id = Scope.ROOT_SCOPE_ID,
                Metadata = new ScopedMetadata(EnumScopedType.Scope)
                {
                    Name = "..",
                },
            });

            Guid controlsScopeId = await _scopeRepo.CreateOrReplaceAsync(controlScope);
            // Control tasks
            foreach (var control in controlScope.Childrens.OfType<AutomationControl>())
            {
                await _tasksRepo.CreateOrReplaceAsync(control);
            }
        }
    }
}
