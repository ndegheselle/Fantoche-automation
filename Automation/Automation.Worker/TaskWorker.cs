using Automation.Dal.Models;
using Automation.Realtime;
using Automation.Realtime.Clients;
using Automation.Shared.Data;

namespace Automation.Worker
{
    public class TaskWorker
    {
        private readonly TasksRealtimeClient _tasksClient;

        public TaskWorker(RedisConnectionManager redis)
        {
            _tasksClient = new TasksRealtimeClient(redis);
        }

        public async Task<EnumTaskState> ExecuteAsync(TaskInstance instance)
        {
            // Load dlls
            // Create instance
            // Start
            // Catch exception
            await Task.Delay(5000);
            return EnumTaskState.Success;
        }
    }
}
