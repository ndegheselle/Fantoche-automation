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

        public async Task RegisterAsync(WorkerInstance instance)
        {
            IDatabase db = _connection.GetDatabase();
            await db.HashSetAsync(_key, instance.Id, JsonSerializer.Serialize(instance));
        }

        public async Task UnregisterAsync(WorkerInstance instance)
        {
            IDatabase db = _connection.GetDatabase();
            await db.HashDeleteAsync(_key, instance.Id);
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
    }
}
