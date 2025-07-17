using Automation.Dal.Models;
using Automation.Plugins.Shared;

namespace Automation.Worker.Executor
{
    public interface ITaskResolver
    {
        Task<BaseAutomationTask> ResolveAsync(Guid id);
    }

    public abstract class BaseTaskExecutor
    {
        private readonly ITaskResolver _taskResolver;

        protected BaseTaskExecutor(ITaskResolver taskResolver)
        {
            _taskResolver = taskResolver;
        }

        /// <summary>
        /// Execute a task, return the finished task state
        /// </summary>
        /// <param name="instance">Instance of the task to execute</param>
        /// <param name="progress">Progress of the task</param>
        /// <returns>A task instance representing the task execution</returns>
        public abstract Task<TaskInstance> ExecuteAsync(TaskTarget target, TaskParameters parameters, IProgress<TaskInstanceNotification>? progress = null);
        public abstract Task<TaskInstance> ExecuteAsync(Graph target, TaskParameters parameters, IProgress<TaskInstanceNotification>? progress = null);

        public async Task<TaskInstance> ExecuteAsync(Guid id, TaskParameters parameters, IProgress<TaskInstanceNotification>? progress = null)
        {
            BaseAutomationTask resolved = await _taskResolver.ResolveAsync(id);

            if (resolved is AutomationTask task)
                return await ExecuteAsync(task.Target ?? throw new Exception("The automation task doesn't have a target."), parameters, progress);
            else if (resolved is AutomationWorkflow workflow)
                return await ExecuteAsync(workflow.Graph, parameters, progress);
            throw new Exception($"The type '{resolved.GetType()}' is not handled.");
        }
    }
}
