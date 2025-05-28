using Automation.Dal.Models;
using Automation.Dal.Repositories;
using Automation.Shared.Data;
using MongoDB.Driver;

namespace Automation.Supervisor.Api.Database
{
    /// <summary>
    /// Set the default values that should be present in the database
    /// </summary>
    public class DatabaseSeeder
    {
        private readonly ScopesRepository _scopeRepo;

        public DatabaseSeeder(IMongoDatabase database)
        {
            _scopeRepo = new ScopesRepository(database);
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
        }
    }
}
