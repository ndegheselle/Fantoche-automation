using StackExchange.Redis;
using System.Text.Json;

namespace Automation.Realtime.Base
{
    public class RedisSubscriber<T>
    {
        protected readonly IConnectionMultiplexer _connection;
        protected readonly string _publishChannel;
        private readonly RedisChannel _channel;

        private List<IProgress<T>> _subscribed = [];

        public RedisSubscriber(IConnectionMultiplexer connection, string publishChannel)
        {
            _connection = connection;
            _publishChannel = publishChannel;
            _channel = new RedisChannel(_publishChannel, RedisChannel.PatternMode.Literal);
        }

        /// <summary>
        /// Subscribe to the publisher notifications.
        /// Returns a subscription ID that can be used to unsubscribe this specific callback.
        /// </summary>
        /// <param name="callback"></param>
        public void Subscribe(IProgress<T> callback)
        {
            if (_subscribed.Count <= 0)
                _connection.GetSubscriber().Subscribe(_channel, OnMessageReceived);
            _subscribed.Add(callback);
        }

        /// <summary>
        /// Unsubscribe a specific subscriber
        /// </summary>
        public void Unsubscribe(IProgress<T> callback)
        {
            _subscribed.Remove(callback);
            if (_subscribed.Count <= 0)
                _connection.GetSubscriber().Unsubscribe(_channel);
        }

        /// <summary>
        /// Unsubscribe all subscribers from the channel.
        /// </summary>
        public void UnsubscribeAll()
        {
            _subscribed.Clear();
            _connection.GetSubscriber().Unsubscribe(_channel);
        }

        /// <summary>
        /// Publish a message to the Redis channel.
        /// </summary>
        /// <param name="message">The message to publish.</param>
        public void Publish(string message)
        {
            _connection.GetSubscriber().Publish(_channel, JsonSerializer.Serialize(message));
        }

        private void OnMessageReceived(RedisChannel channel, RedisValue value)
        {
            foreach(var subscriber in _subscribed)
                subscriber.Report(JsonSerializer.Deserialize<T>(value.ToString()) ?? throw new Exception($"Can't deserialize '{value}' to type '{typeof(T)}'"));
        }
    }
}