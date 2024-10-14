using Automation.Realtime.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace Automation.Realtime.Clients
{
    public class WorkerRealtimeClient
    {
        private readonly string _key;
        private ConnectionMultiplexer _connection;

        public WorkerRealtimeClient(RedisConnectionManager manager)
        {
            _key = "workers";
            _connection = manager.Connection;
        }

        public async Task AddWorkerAsync(WorkerInstance instance)
        {
            IDatabase db = _connection.GetDatabase();
            await db.HashSetAsync(_key, instance.Id, JsonSerializer.Serialize(instance));
        }

        public async Task<IEnumerable<WorkerInstance>> GetWorkersAsync()
        {
            IDatabase db = _connection.GetDatabase();
            var workerIds = await db.HashKeysAsync(_key);
            var workerData = await db.HashGetAsync(_key, workerIds.ToArray());
            return workerData.Select(
                x => JsonSerializer.Deserialize<WorkerInstance>(x.ToString()) ??
                    throw new Exception("Unknown worker format."));
        }

        public async Task AddWorkerTask(string workerId, string taskData)
        {
            var db = _connection.GetDatabase();
            string key = $"worker:{workerId}:tasks";
            await db.ListRightPushAsync(key, taskData);
        }

        public async Task<string?> GetWorkerTask(string workerId)
        {
            var db = _connection.GetDatabase();
            string key = $"worker:{workerId}:tasks";
            return await db.ListLeftPopAsync(key);
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
