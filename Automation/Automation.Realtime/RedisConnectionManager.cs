using StackExchange.Redis;

namespace Automation.Realtime
{
    public class RedisConnectionManager
    {
        public ConnectionMultiplexer Connection { get; private set; }
        public RedisConnectionManager(string connectionString) 
        {
            Connection = ConnectionMultiplexer.Connect(connectionString);
        }
    }
}
