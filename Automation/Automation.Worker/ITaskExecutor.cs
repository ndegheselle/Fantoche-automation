using Automation.Dal.Models;
using Automation.Plugins.Shared;

namespace Automation.Worker
{
    public interface ITaskExecutor
    {
        /// <summary>
        /// Execute a task, return the finished task state
        /// </summary>
        /// <param name="automationTask">Task to execute</param>
        /// <param name="parameters">Context of the execution</param>
        /// <param name="progress">Progress of the task</param>
        /// <returns>A task instance representing the task execution</returns>
        Task<TaskInstance> ExecuteAsync(AutomationTask automationTask, TaskParameters parameters, IProgress<TaskProgress>? progress);
    }
}
