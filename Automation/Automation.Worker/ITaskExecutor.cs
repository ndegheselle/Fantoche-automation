using Automation.Plugins.Shared;
using Automation.Shared.Data;

namespace Automation.Worker
{
    public interface ITaskExecutor
    {
        /// <summary>
        /// Execute a task, return the task instance in a finished state.
        /// </summary>
        /// <param name="instance">Task instance to execute</param>
        /// <returns>Finished task instance</returns>
        Task<EnumTaskState> ExecuteAsync(TargetedPackage package, TaskContext context, IProgress<TaskProgress> progress);
    }
}
