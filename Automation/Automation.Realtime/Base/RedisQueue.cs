using StackExchange.Redis;
using System.Text.Json;

namespace Automation.Realtime.Base
{
    public class RedisQueue<T> : RedisSubscriber<T> where T : struct
    {
        private string _queueNewChannel => $"{_publishChannel}:new";

        public RedisQueue(ConnectionMultiplexer connection, string queueChannel) : base(connection, queueChannel)
        {}

        /// <summary>
        /// Add an element to the queue.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="data"></param>
        public async void QueueAsync(T data)
        {
            IDatabase db = _connection.GetDatabase();
            ISubscriber sub = _connection.GetSubscriber();
            await db.ListLeftPushAsync(_publishChannel, JsonSerializer.Serialize(data), flags: CommandFlags.FireAndForget);
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
            var value = await db.ListRightPopAsync(_publishChannel);

            if (!value.HasValue)
                return null;

            return JsonSerializer.Deserialize<T>(value.ToString());
        }

        public async Task<long> GetQueueLengthAsync()
        {
            IDatabase db = _connection.GetDatabase();
            return await db.ListLengthAsync(_publishChannel);
        }

        public async Task DeleteQueueAsync()
        {
            IDatabase db = _connection.GetDatabase();
            await db.KeyDeleteAsync(_publishChannel);
            // XXX : delete new channel ?
        }
    }
}
