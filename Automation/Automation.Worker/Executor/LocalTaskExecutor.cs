using Automation.Dal.Models;
using Automation.Plugins.Shared;
using Automation.Shared.Data;
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

            ITask? task = null;
            if (instance.Task.Target is ClassTarget classTarget)
            {
                Type controlType = ControlsTasks.Availables[classTarget.Class];
                task = Activator.CreateInstance(controlType) as ITask ?? throw new Exception();
            }
            else if (instance.Task.Target is PackageClassTarget packageTarget)
            {
                task = await _packages.CreateTaskInstanceAsync(packageTarget);
            }

            if (task == null)
                throw new Exception("Task without a target can't be executed.");

            return await ExecuteAsync(instance, task, progress);
        }

        private async Task<AutomationTaskInstance> ExecuteAsync(AutomationTaskInstance instance, ITask task, IProgress<TaskProgress>? progress = null)
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
