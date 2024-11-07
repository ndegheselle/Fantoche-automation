using Automation.Plugins.Shared;
using Automation.Shared.Data;
using StackExchange.Redis;

namespace Automation.Realtime.Clients
{
    public class TasksRealtimeClient
    {
        private string _instanceProgressChannel = "task:instance:{0}:progress";
        private string _instanceLifecyleChannel = "task:instance:{0}:lifecycle";

        public RedisPublisher<TaskProgress> Progress { get; private set; }
        public RedisPublisher<EnumTaskState> Lifecycle { get; private set; }

        public TasksRealtimeClient(ConnectionMultiplexer connection, Guid instanceId) { 
            Progress = new RedisPublisher<TaskProgress>(connection, $"tasks:instance:{instanceId}:progress");
            Lifecycle = new RedisPublisher<EnumTaskState>(connection, $"tasks:instance:{instanceId}:lifecycle");
        }
    }
}
