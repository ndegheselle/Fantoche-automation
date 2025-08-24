using Automation.Dal.Models;
using Automation.Plugins.Shared;
using Automation.Worker.Control;

namespace Automation.Worker.Executor
{
    public interface ITaskExecutor
    {
        public Task<TaskInstance> ExecuteAsync(
            TaskInstance instance,
            IProgress<TaskInstanceNotification>? progress = null);
    }

    internal class TargetExecutor
    {
        private readonly Packages.IPackageManagement _packages;
        public TargetExecutor(Packages.IPackageManagement packageManagement)
        {
            _packages = packageManagement;
        }

        public async Task<EnumTaskState> ExecuteAsync(TaskTarget target, object parameters, IProgress<TaskInstanceNotification>? progress = null)
        {
            ITask? task = null;
            if (target is ClassTarget classTarget)
            {
                Type controlType = ControlTasks.Availables[classTarget.TargetClass].Type;
                task = Activator.CreateInstance(controlType) as ITask ?? throw new Exception();
            }
            else if (target is PackageClassTarget packageTarget)
            {
                task = await _packages.CreateTaskInstanceAsync(packageTarget);
            }

            if (task == null)
                throw new Exception("Could not create the task.");

            return await ExecuteAsync(task, parameters, progress);
        }

        public async Task<EnumTaskState> ExecuteAsync(ITask task, object parameters, IProgress<TaskInstanceNotification>? progress = null)
        {
            try
            {
                TaskResult result = await task.DoAsync(
                    parameters,
                    progress);
                return result.State;
            }
            catch
            { }
            return EnumTaskState.Failed;
        }
    }
}
