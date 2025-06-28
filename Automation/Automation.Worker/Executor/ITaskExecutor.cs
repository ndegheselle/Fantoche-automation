using Automation.Dal.Models;
using Automation.Plugins.Shared;

namespace Automation.Worker.Executor
{
    public interface ITaskExecutor
    {
        /// <summary>
        /// Execute a task, return the finished task state
        /// </summary>
        /// <param name="instance">Instance of the task to execute</param>
        /// <param name="progress">Progress of the task</param>
        /// <returns>A task instance representing the task execution</returns>
        Task<AutomationTaskInstance> ExecuteAsync(AutomationTaskInstance instance, IProgress<TaskInstanceNotification>? progress = null);
    }
}
