using Automation.Dal.Models;
using Automation.Plugins.Shared;
using Automation.Shared.Data;
using Automation.Shared.Packages;
using System.Threading.Tasks;

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

        /// <inheritdoc />
        public Task<AutomationTaskInstance> ExecuteAsync(AutomationTask automationTask, TaskParameters parameters, IProgress<TaskProgress>? progress = null)
        {
            AutomationTaskInstance taskInstance = new AutomationTaskInstance(automationTask.Id, parameters);
            return ExecuteAsync(taskInstance, automationTask.Package, progress);
        }

        public async Task<AutomationTaskInstance> ExecuteAsync(AutomationTaskInstance taskInstance, TargetedPackageClass? package, IProgress<TaskProgress>? progress = null)
        {
            ITask task = await _packages.CreateTaskInstanceAsync(package ?? throw new Exception("Task without target package can't be executed."));
            try
            {
                taskInstance.State = await task.DoAsync(
                    taskInstance.Parameters,
                    progress
                );
            }
            catch
            {
                taskInstance.State = EnumTaskState.Failed;
            }

            return taskInstance;
        }
    }
}
