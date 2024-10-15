using Automation.Realtime.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace Automation.Realtime.Clients
{
    public class TasksRealtimeClient
    {
        private ConnectionMultiplexer _connection;

        public TasksRealtimeClient(RedisConnectionManager manager)
        {
            _connection = manager.Connection;
        }

        public void Notify(string workerId, Guid taskId)
        {
            ISubscriber sub = _connection.GetSubscriber();
            var channel = new RedisChannel($"worker:{workerId}:tasks", RedisChannel.PatternMode.Literal);
            sub.Publish(channel, taskId.ToString());
        }

        public void Subscribe(string workerId, Action<Guid> callback)
        {
            var channel = new RedisChannel($"worker:{workerId}:tasks", RedisChannel.PatternMode.Literal);
            _connection.GetSubscriber()
                .Subscribe(channel, (channel, message) =>
                {
                    callback.Invoke(Guid.Parse(message.ToString()));
                });
        }
    }
}
