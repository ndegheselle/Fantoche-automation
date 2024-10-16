using Automation.Dal.Models;
using Automation.Shared.Data;
using MongoDB.Driver;

namespace Automation.Dal.Repositories
{
    public class TaskIntanceRepository : BaseCrudRepository<TaskInstance>
    {
        public TaskIntanceRepository(IMongoDatabase database) : base(database, "task_instance")
        {
        }

        public virtual async Task<IEnumerable<TaskInstance>> GetByWorkerAndStateAsync(
            string workerId,
            IEnumerable<EnumTaskState> states)
        {
            var filter = Builders<TaskInstance>.Filter
                .And(
                    Builders<TaskInstance>.Filter.Eq(x => x.WorkerId, workerId),
                    Builders<TaskInstance>.Filter.In(x => x.State, states));

            return await _collection.Find(filter).ToListAsync();
        }
    }
}
