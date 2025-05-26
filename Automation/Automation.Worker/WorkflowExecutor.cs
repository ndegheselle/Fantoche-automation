using Automation.Dal.Models;
using Automation.Dal.Repositories;
using Automation.Shared.Data;
using System.Linq;

namespace Automation.Worker
{
    public class WorkflowExecutor
    {
        private readonly GraphsRepository _graphRepo;

        public async Task ExecuteAsync(AutomationWorkflow workflow)
        {
            var graph = await _graphRepo.GetByWorkflowIdAsync(workflow.Id);

            var startNode = graph.Nodes.OfType<GraphControlTask>().Where(x => x.Type == EnumControlTaskType.Start).Single();
            var nexts = graph.Connections.Where(x => x.SourceId = );

            // Get the graph
            // Get first node
            // Assign to worker
            // Wait for worker to finish (globally listen to task states)
            // Check connections
            // goto assign
        }
    }
}
