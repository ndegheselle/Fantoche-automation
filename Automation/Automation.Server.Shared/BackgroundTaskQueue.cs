using System.Threading.Channels;

namespace Automation.Server.Shared
{
    public interface IBackgroundTaskQueue<T>
    {
        ValueTask EnqueueAsync(T workItem);
        ValueTask<T> DequeueAsync(CancellationToken cancellationToken);
    }

    public class BackgroundTaskQueue<T> : IBackgroundTaskQueue<T>
    {
        private readonly Channel<T> _queue;

        public BackgroundTaskQueue(int capacity = 100)
        {
            var options = new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            _queue = Channel.CreateBounded<T>(options);
        }

        public async ValueTask EnqueueAsync(T workItem)
        {
            await _queue.Writer.WriteAsync(workItem);
        }

        public async ValueTask<T> DequeueAsync(CancellationToken cancellationToken)
        {
            return await _queue.Reader.ReadAsync(cancellationToken);
        }
    }
}
