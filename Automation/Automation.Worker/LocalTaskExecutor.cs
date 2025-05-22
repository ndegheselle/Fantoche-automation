using Automation.Dal.Models;
using Automation.Plugins.Shared;
using Automation.Shared.Packages;

namespace Automation.Worker
{
    /// <summary>
    /// Execute a automationTask.
    /// </summary>
    public class LocalTaskExecutor : ITaskExecutor
    {
        private readonly IPackageManagement _packages;
        public LocalTaskExecutor(IPackageManagement packageManagement)
        {
            _packages = packageManagement;
        }

        public async Task<EnumTaskState> ExecuteAsync(AutomationTask automationTask, TaskContext context, IProgress<TaskProgress> progress)
        {
            ITask task = await _packages.CreateTaskInstanceAsync(automationTask.Package ?? throw new Exception("Task without target package can't be executed."));
            try
            {
                return await task.DoAsync(
                    context,
                    progress
                );
            }
            catch
            {}

            return EnumTaskState.Failed;
        }
    }
}
