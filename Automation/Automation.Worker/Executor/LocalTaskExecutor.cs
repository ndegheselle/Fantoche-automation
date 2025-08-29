using Automation.Dal;
using Automation.Models;
using Automation.Dal.Repositories;
using Automation.Plugins.Shared;
using Automation.Realtime.Models;
using Automation.Shared.Data.Task;
using Automation.Worker.Packages;

namespace Automation.Worker.Executor
{
    /// <summary>
    /// Execute a task localy
    /// </summary>
    public class LocalTaskExecutor : ITaskExecutor
    {
        private readonly TasksRepository _tasksRepo;
        private readonly IPackageManagement _packages;
        public LocalTaskExecutor(DatabaseConnection connection, IPackageManagement packageManagement)
        {
            _tasksRepo = new TasksRepository(connection);
            _packages = packageManagement;
        }

        /// <summary>
        /// Execute a task instance
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<TaskInstance> ExecuteAsync(
            TaskInstance instance,
            IProgress<TaskInstanceNotification>? progress = null)
        {
            Shared.Data.Task.BaseAutomationTask baseTask = await _tasksRepo.GetByIdAsync(instance.TaskId);

            if (baseTask is not Shared.Data.Task.AutomationTask task)
                throw new Exception("Task is not a valid automation task.");

            if (task.Target is not PackageClassTarget package)
                throw new Exception("Task target is not a package.");

            instance.StartedAt = DateTime.UtcNow;
            EnumTaskState resultState = await ExecuteAsync(package, instance, progress);
            instance.FinishedAt = DateTime.UtcNow;
            instance.State = resultState;

            return instance;
        }

        /// <summary>
        /// Execute a package target.
        /// </summary>
        /// <param name="packageTarget"></param>
        /// <param name="instance"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        private async Task<EnumTaskState> ExecuteAsync(PackageClassTarget packageTarget, TaskInstance instance, IProgress<TaskInstanceNotification>? progress = null)
        {
            ITask task = await _packages.CreateTaskInstanceAsync(packageTarget);
            try
            {
                Progress<TaskNotification>? taskProgress = null;
                if (progress != null)
                {
                    taskProgress = new Progress<TaskNotification>((notification) => progress.Report(new Shared.Data.TaskInstanceNotification() { InstanceId = instance.Id, Data = notification }));
                }

                await task.DoAsync(instance.Parameters, taskProgress);
                return EnumTaskState.Completed;
            }
            catch
            { }
            return EnumTaskState.Failed;
        }
    }
}
