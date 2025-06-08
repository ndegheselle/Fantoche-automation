namespace Automation.Realtime.Clients
{
    public class RealtimeClients
    {
        public WorkersRealtimeClient Workers { get; }
        public LifecycleRedisPublisher Lifecycle { get; }
        public ProgressRedisPublisher Progress { get; }

        public RealtimeClients(RedisConnectionManager connection)
        {
            Workers = new WorkersRealtimeClient(connection.Connection);
            Lifecycle = new LifecycleRedisPublisher(connection.Connection);
            Progress = new ProgressRedisPublisher(connection.Connection);
        }
    }
}
