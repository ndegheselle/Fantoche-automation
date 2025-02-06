using Automation.Dal.Models;
using Automation.Shared.Data;
using MongoDB.Driver;

namespace Automation.Dal.Repositories
{
    public class GraphsRepository : BaseCrudRepository<Graph>
    {
        public GraphsRepository(IMongoDatabase database) : base(database, "graphs")
        {
        }

        public async Task<Graph?> GetByWorkflowId(Guid workflowId)
        {
            return await _collection.Find(e => e.WorkflowId == workflowId).FirstOrDefaultAsync();
        }
    }
}
