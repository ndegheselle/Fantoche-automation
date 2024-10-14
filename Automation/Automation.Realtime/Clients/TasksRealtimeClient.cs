using Automation.Realtime.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace Automation.Realtime.Clients
{
    public class WorkerTasksRealtimeClient
    {
        private readonly string _key;
        private ConnectionMultiplexer _connection;

        public WorkerTasksRealtimeClient(RedisConnectionManager manager, string workerId)
        {
            _key = $"worker:{workerId}:tasks";
            _connection = manager.Connection;
        }

        public async Task AddTask(string taskData)
        {
            var db = _connection.GetDatabase();
            await db.ListRightPushAsync(_key, taskData);
        }

        public async Task<string?> GetNextTask()
        {
            var db = _connection.GetDatabase();
            return await db.ListLeftPopAsync(_key);
        }

        public void SubscribeToTasks(string workerId, Action callback)
        {
            var db = _connection.GetDatabase();
            string key = $"worker:{workerId}:tasks";
            var channel = new RedisChannel(key, RedisChannel.PatternMode.Literal);
            _connection.GetSubscriber()
                .Subscribe(channel, (channel, message) =>
                {
                    callback.Invoke();
                    // Remove task
                    db.ListRemoveAsync(key, message);
                });
        }
    }
}
