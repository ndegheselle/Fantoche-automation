using Automation.Dal.Models;
using Automation.Shared;
using MongoDB.Driver;

namespace Automation.Dal.Repositories
{
    public class WorkflowRepository : BaseCrudRepository<WorkflowNode>
    {
        public WorkflowRepository(IMongoDatabase database) : base(database, "tasks")
        {}

        public async override Task<WorkflowNode?> GetByIdAsync(Guid id)
        {
            var task = await _collection.Find(e => e.Id == id).FirstAsync();

            if (task is not WorkflowNode workflow)
            {
                throw new Exception($"{id} does not correspond to a workflow.");
            }

            // If workflow get all related tasks and fill nodes list
            var relatedTasks = workflow.Nodes.OfType<RelatedTaskNode>();
            var fullRelatedTasks = (await GetByIdsAsync(relatedTasks.Select(x => x.Id))).ToDictionary(x => x.Id, x => x);
            foreach (var related in relatedTasks)
            {
                related.Node = fullRelatedTasks[related.Id];
            }

            return workflow;
        }
    }
}
