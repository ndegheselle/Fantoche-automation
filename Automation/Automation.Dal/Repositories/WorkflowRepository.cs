using Automation.Dal.Models;
using Automation.Shared;
using Automation.Shared.Contracts;
using MongoDB.Driver;

namespace Automation.Dal.Repositories
{
    public class WorkflowRepository : BaseCrudRepository<WorkflowNode>, IWorkflowRepository<WorkflowNode>
    {
        public WorkflowRepository(IMongoDatabase database) : base(database, "Workflows")
        {}

        public async Task<IEnumerable<WorkflowNode>> GetByScopeAsync(Guid scopeId)
        {
            var projection = Builders<WorkflowNode>.Projection.Include(s => s.Id).Include(s => s.Name);
            return await _collection.Find(e => e.ScopeId == scopeId).Project<WorkflowNode>(projection).ToListAsync();
        }
    }
}
