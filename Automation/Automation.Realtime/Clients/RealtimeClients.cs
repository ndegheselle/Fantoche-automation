namespace Automation.Realtime.Clients
{
    public class RealtimeClients
    {
        public WorkersRealtimeClient Workers { get; }
        public LifecycleRedisPublisher Lifecycle { get; }
        public NotificationRedisPublisher Notifications { get; }

        public RealtimeClients(RedisConnectionManager connection)
        {
            Workers = new WorkersRealtimeClient(connection.Connection);
            Lifecycle = new LifecycleRedisPublisher(connection.Connection);
            Notifications = new NotificationRedisPublisher(connection.Connection);
        }
    }
}
