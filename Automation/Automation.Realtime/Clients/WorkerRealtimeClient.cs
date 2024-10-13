using Automation.Realtime.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace Automation.Realtime.Clients
{
    public class WorkerRealtimeClient
    {
        private readonly string _collection;
        private ConnectionMultiplexer _connection;

        public WorkerRealtimeClient(RedisConnectionManager manager)
        {
            _collection = "workers";
            _connection = manager.Connection;
        }

        public async Task AddWorkerAsync(WorkerInstance instance)
        {
            IDatabase db = _connection.GetDatabase();
            await db.HashSetAsync(_collection, instance.Id, JsonSerializer.Serialize(instance));
        }

        public async Task<IEnumerable<WorkerInstance>> GetWorkersAsync()
        {
            IDatabase db = _connection.GetDatabase();
            var workerIds = await db.HashKeysAsync(_collection);
            var workerData = await db.HashGetAsync(_collection, workerIds.ToArray());
            return workerData.Select(x => JsonSerializer.Deserialize<WorkerInstance>(x.ToString()) ?? throw new Exception("Unknown worker format."));
        }
    }
}
