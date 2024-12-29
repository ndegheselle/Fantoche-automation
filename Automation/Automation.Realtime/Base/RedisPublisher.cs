using StackExchange.Redis;
using System.Text.Json;

namespace Automation.Realtime.Base
{
    public class RedisPublisher<T>
    {
        private ConnectionMultiplexer _connection;
        private readonly string _publishChannel;

        public RedisPublisher(ConnectionMultiplexer connection, string publishChannel)
        {
            _connection = connection;
            _publishChannel = publishChannel;
        }

        /// <summary>
        /// Notify suscribers.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="data"></param>
        public void Notify(T data)
        {
            ISubscriber sub = _connection.GetSubscriber();
            var channel = new RedisChannel(_publishChannel, RedisChannel.PatternMode.Literal);
            sub.Publish(channel, JsonSerializer.Serialize(data));
        }

        /// <summary>
        /// Subscribe to the publisher notifications.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="callback"></param>
        public void Subscribe(Action<T?> callback)
        {
            var channel = new RedisChannel(_publishChannel, RedisChannel.PatternMode.Literal);
            _connection.GetSubscriber()
                .Subscribe(
                    channel,
                    (channel, message) =>
                    {
                        callback.Invoke(JsonSerializer.Deserialize<T>(message.ToString()));
                    });
        }

        public void Unsubscribe()
        {
            var channel = new RedisChannel(_publishChannel, RedisChannel.PatternMode.Literal);
            var subscriber = _connection.GetSubscriber();
            subscriber.Unsubscribe(channel);
        }
    }
}
