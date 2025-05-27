using Automation.Dal.Models;
using Automation.Dal.Repositories;
using Automation.Plugins.Shared;
using Automation.Shared.Data;
using static System.Net.Mime.MediaTypeNames;

namespace Automation.Worker
{
    public class WorkflowExecutor
    {
        private readonly GraphsRepository _graphsRepo;
        private readonly TasksRepository _tasksRepo;

        private readonly ITaskExecutor _executor;

        public async Task<AutomationTaskInstance> ExecuteWorkflowAsync(AutomationWorkflow workflow, TaskParameters parameters, IProgress<TaskProgress>? progress = null)
        {
            AutomationTaskInstance workflowInstance = new AutomationTaskInstance(workflow.Id, parameters);
            var graph = await LoadGraph(workflow.Id);

            graph.RefreshConnections();

            var startNode = graph.Nodes.OfType<GraphControlTask>().Where(x => x.Type == EnumControlTaskType.Start).Single();

            ExecuteTaskAsync(startNode);
        }

        private async Task ExecuteTaskAsync(Graph graph, AutomationTask task)
        {
            AutomationTaskInstance currentInstance = await _executor.ExecuteAsync(task, new TaskParameters("", ""));
            var nextConnections = graph.Connections.Where(x => x.Source?.Parent?.Id == task.Id);
            foreach (var connection in nextConnections)
            {
                if (connection.Target?.Parent == null)
                    throw new Exception($"Could not resolve connection target node ['{connection.SourceId}' <-> '{connection.TargetId}'].");

                // TODO : pass next instance to the next task
                // TODO : handle task state and errors (if any) to stop the workflow execution if needed
                // TODO : handle task outputs and pass them to the next task if needed

                AutomationTask nextTask = graph.Tasks[connection.Target.Parent.Id];
                if (nextTask is AutomationWorkflow subWorkflow)
                    await ExecuteWorkflowAsync(subWorkflow, new TaskParameters("", ""));
                else
                    await ExecuteTaskAsync(graph, nextTask);
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

            graph.RefreshConnections();

            var startNode = graph.Nodes.OfType<GraphControlTask>().Where(x => x.Type == EnumControlTaskType.Start).Single();
            var nexts = graph.Connections.Where(x => x.Source?.Parent?.Id == startNode.Id);

            var graphTasks = await _tasksRepo.GetByIdsAsync(graph.Nodes.OfType<GraphTask>().Select(x => x.TaskId).Distinct());
            graph.Tasks = graphTasks.ToDictionary(x => x.Id, x => x);

            return graph;
        }
    }
}
