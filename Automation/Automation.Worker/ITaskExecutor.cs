using Automation.Dal.Models;

namespace Automation.Worker
{
    public interface ITaskExecutor
    {
        /// <summary>
        /// Execute a task, return the task instance in a finished state.
        /// </summary>
        /// <param name="instance">Task instance to execute</param>
        /// <returns>Finished task instance</returns>
        Task<TaskInstance> ExecuteAsync(TaskInstance instance);
    }
}
