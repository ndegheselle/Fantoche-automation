using Automation.Realtime.Base;
using Automation.Realtime.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace Automation.Realtime.Clients
{
    public class WorkerClientOptions
    {
        public ConnectionMultiplexer Connection {  get; set; }

        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(2);
        public string WorkersChannel { get; set; } = "worker:states";
        public string WorkersHeartbeatChannel { get; set; } = "worker:heartbeats";

        public WorkerClientOptions(ConnectionMultiplexer connection)
        {
            Connection = connection;
        }
    }

    public class WorkerRealtimeClient
    {
        private readonly WorkerClientOptions _options;
        private readonly string _workerId;
        public RedisQueue<Guid> Tasks { get; private set; }
        public RedisQueue<Guid> Cancellation { get; private set; }
        
        public WorkerRealtimeClient(WorkerClientOptions options, string workerId)
        {
            _options = options;
            _workerId = workerId;
            Tasks = new RedisQueue<Guid>(_options.Connection, $"workers:{_workerId}:tasks");
            Cancellation = new RedisQueue<Guid>(_options.Connection, $"workers:{_workerId}:cancellation");
        }

        /// <summary>
        /// Update the worker heartbeat to the current time.
        /// </summary>
        /// <param name="workerId">Target worker id</param>
        public async Task UpdateHeartbeatAsync()
        {
            IDatabase db = _options.Connection.GetDatabase();
            // Store the current timestamp as the heartbeat value
            await db.HashSetAsync(_options.WorkersHeartbeatChannel, _workerId, DateTime.UtcNow.Ticks);
        }

        /// <summary>
        /// Check if a specific worker is still alive (last heartbeat lambda inferior to the timeout delay).
        /// </summary>
        /// <returns>False if the worker is dead</returns>
        public async Task<bool> IsWorkerAliveAsync()
        {
            IDatabase db = _options.Connection.GetDatabase();
            var heartbeat = await db.HashGetAsync(_options.WorkersHeartbeatChannel, _workerId);

            if (!heartbeat.HasValue)
                return false;

            var lastHeartbeatTime = new DateTime(Convert.ToInt64(heartbeat));
            var timeSinceLastHeartbeat = DateTime.UtcNow - lastHeartbeatTime;

            return timeSinceLastHeartbeat <= _options.Timeout;
        }
    }

    public class WorkersRealtimeClient
    {
        private readonly WorkerClientOptions _options;

        public WorkersRealtimeClient(ConnectionMultiplexer connection)
        {
            _options = new WorkerClientOptions(connection);
        }

        public WorkerRealtimeClient ByWorker(string workerId) => new WorkerRealtimeClient(_options, workerId);

        /// <summary>
        /// Update or create a worker instance.
        /// </summary>
        /// <param name="instance"></param>
        public async Task UpdateWorkerAsync(WorkerInstance instance)
        {
            IDatabase db = _options.Connection.GetDatabase();
            await db.HashSetAsync(_options.WorkersChannel, instance.Id, JsonSerializer.Serialize(instance));
            await ByWorker(instance.Id).UpdateHeartbeatAsync();
        }

        /// <summary>
        /// Remove a worker from the list.
        /// </summary>
        /// <param name="instance"></param>
        public async Task RemoveWorkerAsync(string workerId)
        {
            IDatabase db = _options.Connection.GetDatabase();
            await db.HashDeleteAsync(_options.WorkersChannel, workerId);
            // Remove the worker's heartbeat
            await db.HashDeleteAsync(_options.WorkersHeartbeatChannel, workerId);
        }

        /// <summary>
        /// Get all workers (without checking heartbeats).
        /// </summary>
        /// <returns>Liste of worker instances</returns>
        public async Task<IEnumerable<WorkerInstance>> GetWorkersAsync()
        {
            IDatabase db = _options.Connection.GetDatabase();
            var workerIds = await db.HashKeysAsync(_options.WorkersChannel);
            var workerData = await db.HashGetAsync(_options.WorkersChannel, workerIds.ToArray());
            return workerData.Select(
                x => JsonSerializer.Deserialize<WorkerInstance>(x.ToString()) ??
                    throw new Exception("Unknown worker format."));
        }

        /// <summary>
        /// Remove all dead workers
        /// </summary>
        /// <returns></returns>
        public async Task CleanDeadWorkers()
        {
            var db = _options.Connection.GetDatabase();
            var allWorkers = await db.HashGetAllAsync(_options.WorkersHeartbeatChannel);
            var now = DateTime.UtcNow;

            var deadWorkers = allWorkers
                .Where(worker => !worker.Value.HasValue ||
                    (now - new DateTime(Convert.ToInt64(worker.Value))) > _options.Timeout)
                .Select(worker => worker.Name.ToString());

            var removeTask = deadWorkers.Select(RemoveWorkerAsync);
            await Task.WhenAll(removeTask);
        }
    }
}
