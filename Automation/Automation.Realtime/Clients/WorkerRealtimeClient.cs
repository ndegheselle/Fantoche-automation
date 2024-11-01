using Automation.Realtime.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace Automation.Realtime.Clients
{
    public class WorkerRealtimeClient
    {
        private readonly TimeSpan _timeout;
        private readonly string _workersDbKey;
        private readonly string _hearthbeatDbKey;
        private ConnectionMultiplexer _connection;

        public WorkerRealtimeClient(RedisConnectionManager manager)
        {
            _timeout = TimeSpan.FromMinutes(2);
            _workersDbKey = "worker:states";
            _hearthbeatDbKey = "worker:heartbeats";
            _connection = manager.Connection;
        }

        /// <summary>
        /// Update or create a worker instance.
        /// </summary>
        /// <param name="instance"></param>
        public async Task UpdateWorkerAsync(WorkerInstance instance)
        {
            IDatabase db = _connection.GetDatabase();
            await db.HashSetAsync(_workersDbKey, instance.Id, JsonSerializer.Serialize(instance));
            await UpdateHeartbeatAsync(instance.Id);
        }

        /// <summary>
        /// Remove a worker from the list.
        /// </summary>
        /// <param name="instance"></param>
        public async Task RemoveWorkerAsync(string workerId)
        {
            IDatabase db = _connection.GetDatabase();
            await db.HashDeleteAsync(_workersDbKey, workerId);
            // Remove the worker's heartbeat
            await db.HashDeleteAsync(_hearthbeatDbKey, workerId);
        }

        /// <summary>
        /// Get all workers (without checking heartbeats).
        /// </summary>
        /// <returns>Liste of worker instances</returns>
        public async Task<IEnumerable<WorkerInstance>> GetWorkersAsync()
        {
            IDatabase db = _connection.GetDatabase();
            var workerIds = await db.HashKeysAsync(_workersDbKey);
            var workerData = await db.HashGetAsync(_workersDbKey, workerIds.ToArray());
            return workerData.Select(
                x => JsonSerializer.Deserialize<WorkerInstance>(x.ToString()) ??
                    throw new Exception("Unknown worker format."));
        }

        /// <summary>
        /// Update the worker heartbeat to the current time.
        /// </summary>
        /// <param name="workerId">Target worker id</param>
        public async Task UpdateHeartbeatAsync(string workerId)
        {
            IDatabase db = _connection.GetDatabase();
            // Store the current timestamp as the heartbeat value
            await db.HashSetAsync(_hearthbeatDbKey, workerId, DateTime.UtcNow.Ticks);
        }

        /// <summary>
        /// Check if a specific worker is still alive (last heartbeat lambda inferior to the timeout delay).
        /// </summary>
        /// <param name="workerId">Target worker id</param>
        /// <returns>False if the worker is dead</returns>
        public async Task<bool> IsWorkerAliveAsync(string workerId)
        {
            IDatabase db = _connection.GetDatabase();
            var heartbeat = await db.HashGetAsync(_hearthbeatDbKey, workerId);

            if (!heartbeat.HasValue)
                return false;

            var lastHeartbeatTime = new DateTime(Convert.ToInt64(heartbeat));
            var timeSinceLastHeartbeat = DateTime.UtcNow - lastHeartbeatTime;

            return timeSinceLastHeartbeat <= _timeout;
        }

        /// <summary>
        /// Remove all dead workers
        /// </summary>
        /// <returns></returns>
        public async Task CleanDeadWorkers()
        {
            var db = _connection.GetDatabase();
            var allWorkers = await db.HashGetAllAsync(_hearthbeatDbKey);
            var now = DateTime.UtcNow;

            var deadWorkers = allWorkers
                .Where(worker => !worker.Value.HasValue ||
                    (now - new DateTime(Convert.ToInt64(worker.Value))) > _timeout)
                .Select(worker => worker.Name.ToString());

            var removeTask = deadWorkers.Select(RemoveWorkerAsync);
            await Task.WhenAll(removeTask);
        }
    }
}
