using Automation.Plugins.Shared;
using Automation.Realtime.Base;
using StackExchange.Redis;

namespace Automation.Realtime.Clients
{
    public class InstanceProgressRedisPublisher : RedisPublisher<TaskProgress>
    {
        public InstanceProgressRedisPublisher(ConnectionMultiplexer connection, Guid instanceId) : base(
            connection,
            $"instances:{instanceId}:progress")
        {
        }
    }

    public class InstanceLifecycleRedisPublisher : RedisPublisher<EnumTaskState>
    {
        public InstanceLifecycleRedisPublisher(ConnectionMultiplexer connection, Guid instanceId) : base(
            connection,
            $"instances:{instanceId}:lifecycle")
        {
        }

        // TODO : add a timeout for the wait state
        /// <summary>
        /// Wait for a specific state to be reached.
        /// </summary>
        /// <param name="targetState"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<EnumTaskState> WaitStateAsync(EnumTaskState targetState, CancellationToken? cancellationToken = null)
        {
            cancellationToken ??= CancellationToken.None;
            var tcs = new TaskCompletionSource<EnumTaskState>();

            var progress = new Progress<EnumTaskState>((state) =>
            {
                if (state.HasFlag(targetState))
                {
                    tcs.TrySetResult(state);
                }
            });
            Subscribe(progress);

            cancellationToken?.Register(() =>
            {
                tcs.TrySetCanceled();
                Unsubscribe(progress);
            });

            EnumTaskState finalState = await tcs.Task;
            Unsubscribe(progress);
            return finalState;
        }
    }
}