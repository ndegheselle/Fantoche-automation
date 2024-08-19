using Automation.Dal.Models;
using Automation.Shared;
using Automation.Shared.Data;
using MongoDB.Driver;
using static System.Formats.Asn1.AsnWriter;

namespace Automation.Dal.Repositories
{
    public class WorkflowRepository : BaseCrudRepository<WorkflowNode>, IWorkflowRepository<WorkflowNode>
    {
        public WorkflowRepository(IMongoDatabase database) : base(database, "Workflows")
        {}

        public async override Task<WorkflowNode?> GetByIdAsync(Guid id)
        {
            var workflow = await _collection.Find(e => e.Id == id).FirstAsync();

            var taskRepo = new TaskRepository(_database);
            var relatedTasksTask = taskRepo.GetByIdsAsync(workflow.TaskNodeChildrens.Keys);
            var relatedWokflowsTask = GetByIdsAsync(workflow.WorkflowsChildrens.Keys);

            await Task.WhenAll(relatedTasksTask, relatedWokflowsTask);

            workflow.Nodes = [
                .. workflow.Groups,
                .. (await relatedTasksTask).Select(x => new RelatedTaskNode() { Node = x, WorkflowContext = workflow.TaskNodeChildrens[x.Id] }),
                .. (await relatedWokflowsTask).Select(x => new RelatedTaskNode() { Node = x, WorkflowContext = workflow.WorkflowsChildrens[x.Id] }),
            ];

            return workflow;
        }

        public async Task<IEnumerable<WorkflowNode>> GetByScopeAsync(Guid scopeId)
        {
            var projection = Builders<WorkflowNode>.Projection.Include(s => s.Id).Include(s => s.Name);
            return await _collection.Find(e => e.ScopeId == scopeId).Project<WorkflowNode>(projection).ToListAsync();
        }
    }
}
