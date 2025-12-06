namespace Automation.Realtime.Clients
{
    public class RealtimeClients
    {
        /// <summary>
        /// Control workers
        /// </summary>
        public WorkersRealtimeClient Workers { get; }

        /// <summary>
        /// Get instances state changes
        /// </summary>
        public StatesRedisPublisher States { get; }

        /// <summary>
        /// Get tasks notifications during executions
        /// </summary>
        public NotificationRedisPublisher Notifications { get; }

        public RealtimeClients(RedisConnectionManager connection)
        {
            Workers = new WorkersRealtimeClient(connection.Connection);

            States = new StatesRedisPublisher(connection.Connection);
            Notifications = new NotificationRedisPublisher(connection.Connection);
        }
    }
}
