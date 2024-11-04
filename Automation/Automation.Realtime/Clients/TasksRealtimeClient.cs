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
        private string _workerNewChannel = "workers:{0}:tasks:new";
        private string _workerQueueChannel = "workers:{0}:tasks";
        private string _instanceProgressChannel = "task:instance:{0}:progress";
        private string _instanceLifecyleChannel = "task:instance:{0}:lifecycle";

        public TasksRealtimeClient(RedisConnectionManager manager) { _connection = manager.Connection; }

        public async void QueueTask(string workerId, Guid taskId)
        {
            IDatabase db = _connection.GetDatabase();
            ISubscriber sub = _connection.GetSubscriber();
            await db.ListLeftPushAsync(string.Format(_workerQueueChannel, workerId), taskId.ToString(), flags: CommandFlags.FireAndForget);
            var channel = new RedisChannel(string.Format(_workerNewChannel, workerId), RedisChannel.PatternMode.Literal);
            sub.Publish(channel, string.Empty);
        }

        public async Task<Guid?> DequeueTaskAsync(string workerId)
        {
            IDatabase db = _connection.GetDatabase();
            var value = await db.ListRightPopAsync(string.Format(_workerQueueChannel, workerId));

            if (!value.HasValue)
                return null;

            return Guid.Parse(value.ToString());
        }

        public void SubscribeQueue(string workerId, Action callback)
        {
            var channel = new RedisChannel(string.Format(_workerNewChannel, workerId), RedisChannel.PatternMode.Literal);
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
            return await db.ListLengthAsync(string.Format(_workerQueueChannel, workerId));
        }

        public async Task DeleteQueueAsync(string workerId)
        {
            IDatabase db = _connection.GetDatabase();
            await db.KeyDeleteAsync(string.Format(_workerQueueChannel, workerId));
        }

        public void Progress(Guid instanceId, TaskProgress progress)
        {
            ISubscriber sub = _connection.GetSubscriber();
            var channel = new RedisChannel(string.Format(_instanceProgressChannel, instanceId), RedisChannel.PatternMode.Literal);
            sub.Publish(channel, JsonSerializer.Serialize(progress));
        }

        public void SubscribeProgress(Guid instanceId, Action<TaskProgress?> callback)
        {
            var channel = new RedisChannel(string.Format(_taskInstanceChannel, instanceId), RedisChannel.PatternMode.Literal);
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
