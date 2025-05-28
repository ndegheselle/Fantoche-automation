using Automation.Dal.Models;
using Automation.Dal.Repositories;
using Automation.Plugins.Shared;

namespace Automation.Worker.Executor
{
    public class WorkflowExecutor
    {
        private readonly GraphsRepository _graphsRepo;
        private readonly TasksRepository _tasksRepo;

        private readonly RemoteTaskExecutor _executor;

        public async Task ExecuteWorkflowAsync(AutomationTaskInstance instance)
        {
            var graph = await LoadGraph(instance.Id);
            var startNode = graph.Nodes.OfType<GraphControlTask>().Where(x => x.Type == EnumControlTaskType.Start).Single();

            ExecuteNodeAsync(startNode, graph);
        }

        public async Task ExecuteNodeAsync(GraphTask node, Graph graph, AutomationTaskInstance workflowInstance)
        {
            var instance = new AutomationTaskGraphInstance(workflowInstance.TaskId, node, new TaskParameters("", ""));
            if (node is GraphWorkflow)
                await ExecuteWorkflowAsync(instance);
            else
                await _executor.ExecuteAsync(instance);

            var nextConnections = graph.Connections.Where(x => x.Source?.Parent == node);
            foreach (var connection in nextConnections)
            {
                if (connection.Target?.Parent == null)
                    throw new Exception($"Could not resolve connection target node ['{connection.SourceId}' <-> '{connection.TargetId}'].");

                // TODO : pass next instance to the next task
                // TODO : handle task state and errors (if any) to stop the workflow execution if needed
                // TODO : handle task outputs and pass them to the next task if needed

                await ExecuteNodeAsync(connection.Target.Parent, graph, workflowInstance);
            }
        }


        /// <summary>
        /// Load a graph and all it's associated tasks by the workflow ID.
        /// </summary>
        /// <param name="workflowId">Workflow id</param>
        /// <returns>Loaded graph</returns>
        private async Task<Graph> LoadGraph(Guid workflowId)
        {
            var graph = await _graphsRepo.GetByWorkflowIdAsync(workflowId);
            graph.Refresh();
            return graph;
        }
    }
}
