using System.Reflection.Metadata;
using Automation.Dal;
using Automation.Dal.Repositories;
using Automation.Models.Work;
using Automation.Shared.Data.Task;
using Automation.Worker.Control;
using Automation.Worker.Control.Flow;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;

namespace Automation.Worker.Executor
{
    public interface ITaskExecutor
    {
        public Task<JToken?> ExecuteAsync(
            TaskInstance instance,
            JToken? input,
            IProgress<TaskInstanceNotification>? progress = null);
    }

    public class WorkflowExecutor
    {
        private readonly TasksRepository _tasksRepo;

        private readonly ITaskExecutor _executor;

        private readonly AutomationWorkflow _workflow;
        
        public WorkflowExecutor(DatabaseConnection connection, ITaskExecutor executor)
        {
            _tasksRepo = new TasksRepository(connection);
            _executor = executor;
        }

        public void Start(TaskInstance instance)
        {
            JObject context = _workflow.Graph.Execution.GetBaseContext();
            foreach (var start in _workflow.Graph.GetStartNodes())
            {
                Next(start, instance.InputToken, context);
            }
        }

        public void End()
        {
            
        }
        
        public async void Next(BaseGraphTask task, JToken? previous, JObject context)
        {
            var nextTasks = _workflow.Graph.GetNextFrom(task);
            foreach (var next in nextTasks)
            {
                if (next.Settings.WaitAll)
                    next.WaitedInputs.Add(task.Id, previous);
                
                if (_workflow.Graph.CanExecute(next))
                    _ = Execute(next, previous, context);
            }
        }

        public async Task Execute(BaseGraphTask task, JToken? previous, JObject context)
        {
            if (task.Settings.WaitAll)
                task.WaitedInputs.Clear();
            
            // Apply context and create 
            
            JToken? output = null;
            switch (task)
            {
                case GraphWorkflow workflowNode:
                    output = await ExecuteSubWorkflow(workflowNode, previous);
                    break;
                case GraphControl controlNode:
                    output = await ExecuteControl(controlNode, previous);
                    break;
                case GraphTask taskNode:
                    output = await ExecuteTask(taskNode, previous);
                    break;
            }

            Next(node, output, context);
        }
        
        private Task<JToken?> ExecuteSubWorkflow(GraphWorkflow workflowNode, JToken? input)
        {
            
        }
        
        private Task<JToken?> ExecuteControl(GraphControl controlNode, JToken? input)
        {
            if (controlNode.IsEnd())
            {
                
            }
        }

        private Task<JToken?> ExecuteTask(GraphTask taskNode, JToken? input)
        {
            
        }

        /*
        public async Task<TaskInstance> ExecuteWorkflowAsync(TaskInstance instance, IProgress<TaskInstanceNotification>? progress = null)
        {
            BaseAutomationTask baseTask = await _tasksRepo.GetByIdAsync(instance.TaskId);
            if (baseTask is not AutomationWorkflow workflow)
                throw new Exception("Task is not a workflow.");

            workflow.Graph.Refresh();
            var startNode = workflow.Graph.Nodes.OfType<GraphControl>().Single(x => x.TaskId == StartTask.AutomationTask.Id);

            instance.StartedAt = DateTime.UtcNow;
            // We don't need to execute the start node since it doesn't have any logic
            await ExecuteNextAsync(startNode, workflow.Graph, instance);
            instance.FinishedAt = DateTime.UtcNow;
            instance.State = EnumTaskState.Completed;
            return instance;
        }

        private async Task ExecuteNodeAsync(BaseGraphTask node, Graph graph, TaskInstance workflowInstance)
        {
            SubTaskInstance instance = new SubTaskInstance(workflowInstance.Id, node);

            switch (node)
            {
                case GraphWorkflow workflow:
                    await ExecuteWorkflowAsync(instance, null);
                    break;
                case GraphControl controlNode:
                    await ExecuteControlAsync(controlNode);
                    break;
                case GraphTask:
                    await _executor.ExecuteAsync(instance, null);
                    break;
            }

            if (instance.State == EnumTaskState.Completed)
                ExecuteNextAsync(node, graph, workflowInstance);
        }

        private void ExecuteNextAsync(BaseGraphTask node, Graph graph, TaskInstance workflowInstance)
        {
            var nextConnections = graph.Connections.Where(x => x.Source?.Parent == node);
            List<Task> nextTasks = [];
            foreach (var connection in nextConnections)
            {
                if (connection.Target?.Parent == null)
                    throw new Exception($"Could not resolve connection target node ['{connection.SourceId}' <-> '{connection.TargetId}'].");

                // TODO : pass next instance to the next task
                // TODO : handle task state and errors (if any) to stop the workflow execution if needed
                // TODO : handle task outputs and pass them to the next task if needed

                nextTasks.Add(ExecuteNodeAsync(connection.Target.Parent, graph, workflowInstance));
            }
            
            Task.WaitAll(nextTasks);
        }

        /// <summary>
        /// Control task need to be executed by the workflow directly so that it have access to all the workflow execution context.
        /// </summary>
        /// <param name="controlNode"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task ExecuteControlAsync(GraphControl controlNode)
        {
            Type controlType = ControlTasks.AvailablesById[controlNode.TaskId].Type;
            ITaskControl control = Activator.CreateInstance(controlType) as ITaskControl ?? throw new Exception();

            await control.DoAsync(new WorkflowContext());
        }
        
        private async Task ExecuteSubWorkflowAsync(GraphWorkflow workflowNode, Graph graph, TaskInstance workflowInstance)
        {}
        */
    }
}
