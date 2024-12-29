using StackExchange.Redis;
using System.Text.Json;

namespace Automation.Realtime.Base
{
    public class RedisQueue<T> where T : struct
    {
        private ConnectionMultiplexer _connection;
        private readonly string _queueChannel;
        private string _queueNewChannel => $"{_queueChannel}:new";

        public RedisQueue(ConnectionMultiplexer connection, string queueChannel)
        {
            _connection = connection;
            _queueChannel = queueChannel;
        }

        /// <summary>
        /// Add an element to the queue.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="data"></param>
        public async void QueueAsync(T data)
        {
            IDatabase db = _connection.GetDatabase();
            ISubscriber sub = _connection.GetSubscriber();
            await db.ListLeftPushAsync(_queueChannel, JsonSerializer.Serialize(data), flags: CommandFlags.FireAndForget);
            var channel = new RedisChannel(_queueNewChannel, RedisChannel.PatternMode.Literal);
            sub.Publish(channel, string.Empty);
        }

        /// <summary>
        /// Remove an element from the queue.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<T?> DequeueAsync()
        {
            IDatabase db = _connection.GetDatabase();
            var value = await db.ListRightPopAsync(_queueChannel);

            if (!value.HasValue)
                return null;

            return JsonSerializer.Deserialize<T>(value.ToString());
        }

        public async Task<long> GetQueueLengthAsync()
        {
            IDatabase db = _connection.GetDatabase();
            return await db.ListLengthAsync(_queueChannel);
        }

        public async Task DeleteQueueAsync()
        {
            IDatabase db = _connection.GetDatabase();
            await db.KeyDeleteAsync(_queueChannel);
            // XXX : delete new channel ?
        }

        public void SubscribeQueue(Action callback)
        {
            var channel = new RedisChannel(_queueNewChannel, RedisChannel.PatternMode.Literal);
            _connection.GetSubscriber()
                .Subscribe(
                    channel,
                    (channel, message) =>
                    {
                        callback.Invoke();
                    });
        }
    }
}
