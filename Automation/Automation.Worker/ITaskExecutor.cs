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
        /// <param name="context">Context of the execution</param>
        /// <param name="progress">Progress of the task</param>
        /// <returns></returns>
        Task<EnumTaskState> ExecuteAsync(AutomationTask automationTask, TaskContext context, IProgress<TaskProgress> progress);
    }
}
