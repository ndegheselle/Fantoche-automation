using Automation.Dal.Repositories;
using MongoDB.Driver;

namespace Automation.Api.Worker
{
    public static class Initialize
    {
        public static void RegisterWorker(IServiceProvider services)
        {
            IMongoDatabase database = services.GetRequiredService<IMongoDatabase>();
            WorkerRepository repository = new WorkerRepository(database);

            repository.Register();
        }
    }
}
