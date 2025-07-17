using Automation.Dal.Models;
using Automation.Dal.Repositories;
using Automation.Plugins.Shared;
using Automation.Realtime;
using Automation.Realtime.Clients;
using Automation.Worker.Control.Flow;
using MongoDB.Driver;

namespace Automation.Worker.Executor
{
    public class WorkflowExecutor
    {
        private readonly GraphsRepository _graphsRepo;
        private readonly TasksRepository _tasksRepo;

        private readonly RemoteTaskExecutor _executor;

        public WorkflowExecutor(IMongoDatabase database, RealtimeClients clients)
        {
            _graphsRepo = new GraphsRepository(database);
            _tasksRepo = new TasksRepository(database);
            _executor = new RemoteTaskExecutor(database, clients);
        }

        public async Task ExecuteWorkflowAsync(TaskInstance instance)
        {
            var graph = await LoadGraph(instance.Id);
            var startNode = graph.Nodes.OfType<GraphControl>().Single(x => x.TaskId == StartTask.Id);

            // We don't need to execute the start node since it doesn't have any logic
            await ExecuteNextAsync(startNode, graph, instance);

            // TODO : change the instance state to finished
        }

        public async Task ExecuteNodeAsync(GraphTask node, Graph graph, TaskInstance workflowInstance)
        {
            SubTaskInstance instance = new SubTaskInstance(workflowInstance.TaskId, node, new TaskParameters("", ""));
            if (node is GraphWorkflow)
                await ExecuteWorkflowAsync(instance);
            else
                await _executor.ExecuteAsync(instance);

            await ExecuteNextAsync(node, graph, workflowInstance);
        }

        public async Task ExecuteNextAsync(GraphTask node, Graph graph, TaskInstance workflowInstance)
        {
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
