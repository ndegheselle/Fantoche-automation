using Automation.Dal.Models;
using Automation.Dal.Repositories;
using Automation.Plugins.Shared;
using Automation.Worker.Packages;
using MongoDB.Driver;

namespace Automation.Worker.Executor
{
    /// <summary>
    /// Execute a task localy
    /// </summary>
    public class LocalTaskExecutor : ITaskExecutor
    {
        private readonly TasksRepository _tasksRepo;
        private readonly TargetExecutor _targetExecutor;
        public LocalTaskExecutor(IMongoDatabase database, IPackageManagement packageManagement)
        {
            _tasksRepo = new TasksRepository(database);
            _targetExecutor = new TargetExecutor(packageManagement);
        }

        public async Task<TaskInstance> ExecuteAsync(
            TaskInstance instance,
            IProgress<TaskInstanceNotification>? progress = null)
        {
            BaseAutomationTask baseTask = await _tasksRepo.GetByIdAsync(instance.TaskId);

            if (baseTask is not AutomationTask task)
                throw new Exception("Task is not a valid automation task.");

            if (task.Target == null)
                throw new Exception("Task target is not defined.");

            instance.StartedAt = DateTime.UtcNow;
            EnumTaskState resultState = await _targetExecutor.ExecuteAsync(task.Target, instance.Parameters, progress);
            instance.FinishedAt = DateTime.UtcNow;
            instance.State = resultState;

            return instance;
        }
    }
}
