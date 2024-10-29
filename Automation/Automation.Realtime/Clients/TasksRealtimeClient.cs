using Automation.Plugins.Shared;
using Automation.Shared.Data;
using StackExchange.Redis;
using System.Text.Json;
using System.Threading.Tasks;

namespace Automation.Realtime.Clients
{
    public class TasksRealtimeClient
    {
        private ConnectionMultiplexer _connection;

        public TasksRealtimeClient(RedisConnectionManager manager)
        {
            _connection = manager.Connection;
        }

        public void NotifyNewTask(string workerId, Guid taskId)
        {
            ISubscriber sub = _connection.GetSubscriber();
            var channel = new RedisChannel($"worker:{workerId}:tasks", RedisChannel.PatternMode.Literal);
            sub.Publish(channel, taskId.ToString());
        }

        public void SubscribeNewTask(string workerId, Action<Guid> callback)
        {
            var channel = new RedisChannel($"worker:{workerId}:tasks", RedisChannel.PatternMode.Literal);
            _connection.GetSubscriber()
                .Subscribe(channel, (channel, message) =>
                {
                    callback.Invoke(Guid.Parse(message.ToString()));
                });
        }

        public void Progress(Guid taskId, TaskProgress progress)
        {
            ISubscriber sub = _connection.GetSubscriber();
            var channel = new RedisChannel($"task:{taskId}", RedisChannel.PatternMode.Literal);
            sub.Publish(channel, JsonSerializer.Serialize(progress));
        }

        public void SubscribeProgress(Guid taskId, Action<TaskProgress?> callback)
        {
            var channel = new RedisChannel($"task:{taskId}", RedisChannel.PatternMode.Literal);
            _connection.GetSubscriber()
                .Subscribe(channel, (channel, message) =>
                {
                    callback.Invoke(JsonSerializer.Deserialize<TaskProgress>(message.ToString()));
                });
        }
    }
}
