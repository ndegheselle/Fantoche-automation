using Automation.Plugins.Shared;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Automation.Realtime.Clients
{
    public class TasksRealtimeClient
    {
        private ConnectionMultiplexer _connection;

        public TasksRealtimeClient(RedisConnectionManager manager) { _connection = manager.Connection; }

        public async void QueueTask(string workerId, Guid taskId)
        {
            IDatabase db = _connection.GetDatabase();
            ISubscriber sub = _connection.GetSubscriber();
            await db.ListLeftPushAsync($"worker:{workerId}:tasks", taskId.ToString(), flags: CommandFlags.FireAndForget);
            var channel = new RedisChannel($"worker:{workerId}:tasks:update", RedisChannel.PatternMode.Literal);
            sub.Publish(channel, string.Empty);
        }

        public async Task<Guid?> DequeueTaskAsync(string workerId)
        {
            IDatabase db = _connection.GetDatabase();
            var value = await db.ListRightPopAsync($"worker:{workerId}:tasks");

            if (!value.HasValue)
                return null;

            return Guid.Parse(value.ToString());
        }

        public void SubscribeQueue(string workerId, Action callback)
        {
            var channel = new RedisChannel($"worker:{workerId}:tasks:update", RedisChannel.PatternMode.Literal);
            _connection.GetSubscriber()
                .Subscribe(
                    channel,
                    (channel, message) =>
                    {
                        callback.Invoke();
                    });
        }


        public async Task<long> GetQueueTaskLengthAsync(string workerId)
        {
            IDatabase db = _connection.GetDatabase();
            return await db.ListLengthAsync($"worker:{workerId}:tasks");
        }

        public async Task DeleteQueueAsync(string workerId)
        {
            IDatabase db = _connection.GetDatabase();
            await db.KeyDeleteAsync($"worker:{workerId}:tasks");
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
                .Subscribe(
                    channel,
                    (channel, message) =>
                    {
                        callback.Invoke(JsonSerializer.Deserialize<TaskProgress>(message.ToString()));
                    });
        }
    }
}
