using Automation.Plugins.Shared;
using Automation.Shared.Data;
using Automation.Shared.Packages;

namespace Automation.Worker
{
    /// <summary>
    /// Execute a task.
    /// </summary>
    public class LocalTaskExecutor : ITaskExecutor
    {
        private readonly IPackageManagement _packages;
        public LocalTaskExecutor(IPackageManagement packageManagement)
        {
            _packages = packageManagement;
        }

        public async Task<EnumTaskState> ExecuteAsync(TargetedPackage package, TaskContext context, IProgress<TaskProgress> progress)
        {
            ITask task = await _packages.CreateTaskInstanceAsync(package);
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
