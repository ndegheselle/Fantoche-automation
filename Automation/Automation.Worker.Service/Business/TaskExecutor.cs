using Automation.Dal.Models;
using Automation.Plugins.Shared;
using Automation.Realtime;
using Automation.Realtime.Clients;

namespace Automation.Worker.Service.Business
{
    public class TaskExecutor : IExecutor
    {
        // To send task progress to clients
        private readonly TasksRealtimeClient _tasksClient;
        private TaskInstance? _currentInstance = null;

        public TaskExecutor(RedisConnectionManager redis)
        {
            _tasksClient = new TasksRealtimeClient(redis);
        }

        public async Task<EnumTaskState> ExecuteAsync(TaskInstance instance)
        {
            _currentInstance = instance;
            // Load dlls
            // Create instance
            // Start
            // Catch exception
            await Task.Delay(5000);
            return EnumTaskState.Completed;
        }

        public void Progress(TaskProgress progress)
        {
            if (_currentInstance == null)
                return;
            _tasksClient.Progress(_currentInstance.Id, progress);
        }
    }
}
