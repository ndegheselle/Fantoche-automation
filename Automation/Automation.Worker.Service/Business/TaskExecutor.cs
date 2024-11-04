using Automation.Dal.Models;
using Automation.Plugins.Shared;
using Automation.Realtime;
using Automation.Realtime.Clients;
using Automation.Server.Shared.Packages;
using Automation.Shared.Data;

namespace Automation.Worker.Service.Business
{
    public class TaskExecutor : IProgress
    {
        // To send task progress to clients
        private readonly TasksRealtimeClient _tasksClient;
        private readonly IPackageManagement _packages;
        private TaskInstance? _currentInstance = null;

        public TaskExecutor(RedisConnectionManager redis, IPackageManagement packageManagement)
        {
            _packages = packageManagement;
            _tasksClient = new TasksRealtimeClient(redis);
        }

        public async Task<TaskInstance> ExecuteAsync(TaskInstance instance)
        {
            _currentInstance = instance;
            ITask task = await _packages.CreateTaskInstanceAsync(_currentInstance.Target);

            try
            {
                task.Progress = this;
                instance.Results = await task.ExecuteAsync(_currentInstance.Context);
                instance.State = EnumTaskState.Completed;
            }
            catch
            {
                instance.State = EnumTaskState.Failed;
            }

            return instance;
        }

        public void Progress(TaskProgress progress)
        {
            if (_currentInstance == null)
                return;
            _tasksClient.Progress(_currentInstance.Id, progress);
        }
    }
}
