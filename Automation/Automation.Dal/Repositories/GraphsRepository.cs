using Automation.Dal.Models;
using MongoDB.Driver;

namespace Automation.Dal.Repositories
{
    public class GraphsRepository : BaseCrudRepository<Graph>
    {
        public GraphsRepository(IMongoDatabase database) : base(database, "graphs")
        {
        }

        /// <summary>
        /// Get a graph by workflow id
        /// </summary>
        /// <param name="workflowId">Target workflow id</param>
        /// <returns></returns>
        public async Task<Graph> GetByWorkflowIdAsync(Guid workflowId)
        {
            return await _collection.Find(e => e.WorkflowId == workflowId).FirstOrDefaultAsync() ?? throw new Exception($"Unknow graph for workflow id '{workflowId}'");
        }
    }
}
