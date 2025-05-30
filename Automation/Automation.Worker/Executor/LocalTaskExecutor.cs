using Automation.Dal.Models;
using Automation.Plugins.Shared;
using Automation.Worker.Control;
using MongoDB.Bson.Serialization.Serializers;

namespace Automation.Worker.Executor
{
    /// <summary>
    /// Execute a task localy
    /// </summary>
    public class LocalTaskExecutor : ITaskExecutor
    {
        private readonly Packages.IPackageManagement _packages;
        public LocalTaskExecutor(Packages.IPackageManagement packageManagement)
        {
            _packages = packageManagement;
        }

        public async Task<AutomationTaskInstance> ExecuteAsync(AutomationTaskInstance instance, IProgress<TaskProgress>? progress = null)
        {
            if (instance.Task == null)
                throw new ArgumentNullException(nameof(instance.Task), "Local execution of task instance require the Task to be loaded in the instance.");

            if (instance.Task.Package == null)
                throw new Exception("Task without target package can't be executed.");

            ITask task = await _packages.CreateTaskInstanceAsync(instance.Task.Package);
            return await ExecuteAsync(instance, task, progress);
        }

        public async Task<AutomationTaskInstance> ExecuteAsync(AutomationTaskInstance instance, ITask task, IProgress<TaskProgress>? progress = null)
        {
            instance.StartDate = DateTime.Now;
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
