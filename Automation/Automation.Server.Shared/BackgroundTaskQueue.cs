using System.Threading.Channels;

namespace Automation.Server.Shared
{
    public interface IBackgroundQueue<T>
    {
        ValueTask QueueAsync(T item);
        ValueTask<T> DequeueAsync(CancellationToken cancellationToken);
    }

    public class BackgroundTaskQueue<T> : IBackgroundQueue<T>
    {
        private readonly Channel<T> _queue;
        public int Size => _queue.Reader.Count;

        public BackgroundTaskQueue(int capacity)
        {
            var options = new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            _queue = Channel.CreateBounded<T>(options);
        }

        public async ValueTask QueueAsync(T item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            await _queue.Writer.WriteAsync(item);
        }

        public async ValueTask<T> DequeueAsync(CancellationToken cancellationToken)
        {
            return await _queue.Reader.ReadAsync(cancellationToken);
        }
    }
}
