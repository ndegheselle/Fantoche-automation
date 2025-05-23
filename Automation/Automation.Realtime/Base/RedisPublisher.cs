using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Automation.Realtime.Base
{
    public class RedisPublisher<T> : IDisposable
    {
        public delegate void SubscriberCallback(T? content, Guid subscriptionId);

        private readonly IConnectionMultiplexer _connection;
        private readonly string _publishChannel;
        private readonly ConcurrentDictionary<Guid, SubscriberCallback> _subscriptions = new();
        private volatile bool _isChannelSubscribed = false;
        // Specific object for locking the subscription process
        private readonly object _subscriptionLock = new object();
        private bool _disposed = false;

        public RedisPublisher(IConnectionMultiplexer connection, string publishChannel)
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
        public Guid Subscribe(SubscriberCallback callback)
        {
            var subscriptionId = Guid.NewGuid();
            _subscriptions[subscriptionId] = callback;

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
        public void Unsubscribe(Guid subscriptionId)
        {
            _subscriptions.TryRemove(subscriptionId, out _);

            lock (_subscriptionLock)
            {
                // If no more subscribers, unsubscribe from Redis channel
                if (_subscriptions.IsEmpty && _isChannelSubscribed)
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
            try
            {
                var deserializedMessage = JsonSerializer.Deserialize<T>(message.ToString());

                // Notify all active subscribers
                foreach (var subscription in _subscriptions)
                {
                    try
                    {
                        subscription.Value.Invoke(deserializedMessage, subscription.Key);
                    }
                    catch (Exception ex)
                    {
                        // Log exception but don't stop other callbacks
                        Console.WriteLine($"Error in subscriber callback: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deserializing message: {ex.Message}");
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

        ~RedisPublisher()
        {
            Dispose(false);
        }
    }
}