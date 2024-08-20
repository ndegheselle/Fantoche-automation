using Automation.Dal.Models;
using Automation.Shared;
using MongoDB.Driver;

namespace Automation.Dal.Repositories
{
    public class TaskRepository : BaseCrudRepository<TaskNode>, ITaskRepository<TaskNode>
    {
        public TaskRepository(IMongoDatabase database) : base(database, "Tasks")
        {}

        public async override Task<TaskNode?> GetByIdAsync(Guid id)
        {
            var task = await _collection.Find(e => e.Id == id).FirstAsync();

            if (task is not WorkflowNode workflow)
            {
                return task;
            }

            // If workflow get all related tasks and fill nodes list
            var relatedTasks = await GetByIdsAsync(workflow.TaskNodeChildrens.Keys);
            workflow.Nodes = [
                .. workflow.Groups,
                .. relatedTasks.Select(x => new RelatedTaskNode() { Node = x, WorkflowContext = workflow.TaskNodeChildrens[x.Id] })
            ];

            return workflow;
        }

        public async Task<IEnumerable<TaskNode>> GetByScopeAsync(Guid scopeId)
        {
            var projection = Builders<TaskNode>.Projection.Include(s => s.Id).Include(s => s.Name);
            return await _collection.Find(e => e.ScopeId == scopeId).Project<TaskNode>(projection).ToListAsync();
        }
    }
}
