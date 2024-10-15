using Automation.Dal.Models;
using MongoDB.Driver;

namespace Automation.Dal.Repositories
{
    public class TaskIntanceRepository : BaseCrudRepository<TaskNode>
    {
        public TaskIntanceRepository(IMongoDatabase database) : base(database, "task_instance")
        {}
    }
}
