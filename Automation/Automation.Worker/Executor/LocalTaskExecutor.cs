using Automation.Dal.Models;
using Automation.Plugins.Shared;
using Automation.Shared.Packages;

namespace Automation.Worker.Executor
{
    /// <summary>
    /// Execute a task localy
    /// </summary>
    public class LocalTaskExecutor : ITaskExecutor
    {
        private readonly IPackageManagement _packages;
        public LocalTaskExecutor(IPackageManagement packageManagement)
        {
            _packages = packageManagement;
        }

        public async Task<AutomationTaskInstance> ExecuteAsync(AutomationTaskInstance instance, IProgress<TaskProgress>? progress = null)
        {
            if (instance.Task == null)
                throw new ArgumentNullException(nameof(instance.Task) , "Local execution of task instance require the Task to be loaded in the instance.");

            if (instance.Task.Package == null)
                throw new Exception("Task without target package can't be executed.");

            instance.StartDate = DateTime.Now;
            ITask task = await _packages.CreateTaskInstanceAsync(instance.Task.Package);
            try
            {
                instance.State = await task.DoAsync(
                    instance.Parameters,
                    progress
                );
            }
            catch
            {
                instance.State = EnumTaskState.Failed;
            }

            instance.EndDate = DateTime.Now;
            return instance;
        }
    }
}
