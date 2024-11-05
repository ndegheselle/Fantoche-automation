using Automation.Plugins.Shared;
using Automation.Shared.Data;
using StackExchange.Redis;
using System.Text.Json;

namespace Automation.Realtime.Clients
{
    public class TasksRealtimeClient
    {
        private ConnectionMultiplexer _connection;
        private string _instanceProgressChannel = "task:instance:{0}:progress";
        private string _instanceLifecyleChannel = "task:instance:{0}:lifecycle";

        public RedisPublisher<TaskProgress> Progress { get; private set; }
        public RedisPublisher<EnumTaskState> Lifecycle { get; private set; }

        public TasksRealtimeClient(RedisConnectionManager manager) { 
            _connection = manager.Connection;
            Progress = new RedisPublisher<TaskProgress>(_connection, "tasks:instance:{0}:progress");
            Lifecycle = new RedisPublisher<EnumTaskState>(_connection, "tasks:instance:{0}:lifecycle");
        }
    }
}
