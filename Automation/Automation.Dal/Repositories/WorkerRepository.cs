using Automation.Dal.Models;
using MongoDB.Driver;

namespace Automation.Dal.Repositories
{
    public class WorkerRepository : BaseRepository<WorkerInstance>
    {
        public WorkerRepository(IMongoDatabase database) : base(database, "workers")
        { }

        public IEnumerable<WorkerInstance> GetAll()
        {

        }

        public void Register(WorkerInstance worker)
        {

        }

        public void Unregister(string id) {
        }
    }
}
