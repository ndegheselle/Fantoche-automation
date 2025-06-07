using StackExchange.Redis;
using System.Text.Json;

namespace Automation.Realtime.Base
{
    public class RedisSubscriber<T> : IDisposable
    {
        protected readonly IConnectionMultiplexer _connection;
        protected readonly string _publishChannel;
        private readonly List<IProgress<T>> _subscriptions = new();
        private volatile bool _isChannelSubscribed = false;
        // Specific object for locking the subscription process
        private readonly object _subscriptionLock = new object();
        private bool _disposed = false;

        public RedisSubscriber(IConnectionMultiplexer connection, string publishChannel)
        {
            _connection = connection;
            _publishChannel = publishChannel;
        }

        /// <summary>
        /// Subscribe to the publisher notifications.
        /// Returns a subscription ID that can be used to unsubscribe this specific callback.
        /// </summary>
        /// <param name="callback"></param>
        /// <returns>Subscription ID for targeted unsubscription</returns>
        public Guid Subscribe(IProgress<T> callback)
        {
            var subscriptionId = Guid.NewGuid();
            _subscriptions.Add(callback);

            lock (_subscriptionLock)
            {
                if (!_isChannelSubscribed)
                {
                    var channel = new RedisChannel(_publishChannel, RedisChannel.PatternMode.Literal);
                    _connection.GetSubscriber()
                        .Subscribe(channel, OnMessageReceived);
                    _isChannelSubscribed = true;
                }
            }

            return subscriptionId;
        }

        /// <summary>
        /// Unsubscribe a specific subscriber by ID
        /// </summary>
        /// <param name="subscriptionId"></param>
        public void Unsubscribe(IProgress<T> callback)
        {
            _subscriptions.Remove(callback);

            lock (_subscriptionLock)
            {
                // If no more subscribers, unsubscribe from Redis channel
                if (_subscriptions.Count == 0 && _isChannelSubscribed)
                {
                    var channel = new RedisChannel(_publishChannel, RedisChannel.PatternMode.Literal);
                    _connection.GetSubscriber().Unsubscribe(channel);
                    _isChannelSubscribed = false;
                }
            }
        }

        /// <summary>
        /// Unsubscribe all subscribers
        /// </summary>
        public void UnsubscribeAll()
        {
            _subscriptions.Clear();

            lock (_subscriptionLock)
            {
                if (_isChannelSubscribed)
                {
                    var channel = new RedisChannel(_publishChannel, RedisChannel.PatternMode.Literal);
                    _connection.GetSubscriber().Unsubscribe(channel);
                    _isChannelSubscribed = false;
                }
            }
        }

        private void OnMessageReceived(RedisChannel channel, RedisValue message)
        {
            var deserializedMessage = JsonSerializer.Deserialize<T>(message.ToString()) ?? throw new Exception("Can't deserialize received message.");

            // Notify all active subscribers
            foreach (var subscription in _subscriptions)
            {
                subscription.Report(deserializedMessage);
            }
        }

        /// <summary>
        /// Dispose the object and unsubscribe all callbacks.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    UnsubscribeAll();
                }
                _disposed = true;
            }
        }

        ~RedisSubscriber()
        {
            Dispose(false);
        }
    }

    public class RedisPublisher<T> : RedisSubscriber<T>
    {
        public RedisPublisher(IConnectionMultiplexer connection, string publishChannel) : base(connection, publishChannel)
        {
        }

        /// <summary>
        /// Publish a message to the Redis channel.
        /// </summary>
        /// <param name="message">The message to publish.</param>
        public void Publish(T message)
        {
            var serializedMessage = JsonSerializer.Serialize(message);
            var channel = new RedisChannel(_publishChannel, RedisChannel.PatternMode.Literal);
            _connection.GetSubscriber().Publish(channel, serializedMessage);
        }
    }
}