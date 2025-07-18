using Automation.Dal.Models;
using Automation.Plugins.Shared;
using Automation.Shared.Data;
using Automation.Worker.Control;

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

        public async Task<EnumTaskState> ExecuteAsync(TaskTarget target, TaskInstance instance, IProgress<TaskInstanceNotification>? progress = null)
        {
            ITask? task = null;
            if (target is ClassTarget classTarget)
            {
                Type controlType = ControlsTasks.Availables[classTarget.Class];
                task = Activator.CreateInstance(controlType) as ITask ?? throw new Exception();
            }
            else if (target is PackageClassTarget packageTarget)
            {
                task = await _packages.CreateTaskInstanceAsync(packageTarget);
            }

            if (task == null)
                throw new Exception("Task without a target can't be executed.");

            return await ExecuteAsync(task, instance, progress);
        }

        private async Task<EnumTaskState> ExecuteAsync(ITask task, TaskInstance instance, IProgress<TaskInstanceNotification>? progress = null)
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
            return instance.State;
        }
    }
}
