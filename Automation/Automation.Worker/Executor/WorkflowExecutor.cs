using Automation.Dal.Models;
using Automation.Dal.Repositories;
using Automation.Plugins.Shared;
using Automation.Worker.Control.Flow;
using MongoDB.Driver;

namespace Automation.Worker.Executor
{
    public class WorkflowExecutor
    {
        private readonly TasksRepository _tasksRepo;

        private readonly ITaskExecutor _executor;

        public WorkflowExecutor(ITaskExecutor executor, IMongoDatabase database)
        {
            _tasksRepo = new TasksRepository(database);
            _executor = executor;
        }

        public async Task ExecuteWorkflowAsync(TaskInstance instance)
        {
            var task = await _tasksRepo.GetByIdAsync(instance.TaskId);
            task.Graph.Refresh();
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
    }
}
