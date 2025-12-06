using Automation.Realtime.Base;
using Automation.Shared.Data.Task;
using StackExchange.Redis;

namespace Automation.Realtime.Clients
{
    public class StatesRedisPublisher : RedisSubscriber<TaskInstanceState>
    {
        public StatesRedisPublisher(ConnectionMultiplexer connection) : base(
            connection,
            $"instances:states")
        {}

        public void Subscribe(Guid instanceId, IProgress<TaskInstanceState> callback)
        {
            base.Subscribe(new Progress<TaskInstanceState>((instanceState) =>
            {
                if (instanceState.InstanceId == instanceId)
                    callback.Report(instanceState);
            }));
        }

        // TODO : add a timeout for the wait state
        /// <summary>
        /// Wait for a specific state to be reached.
        /// </summary>
        /// <param name="instanceId"></param>
        /// <param name="targetState"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<EnumTaskState> WaitStateAsync(Guid instanceId, EnumTaskState targetState, CancellationToken? cancellationToken = null)
        {
            cancellationToken ??= CancellationToken.None;
            var tcs = new TaskCompletionSource<EnumTaskState>();

            var progress = new Progress<TaskInstanceState>((instanceState) =>
            {
                if (instanceState.State.HasFlag(targetState))
                    tcs.TrySetResult(instanceState.State);
            });
            Subscribe(instanceId, progress);

            cancellationToken?.Register(() =>
            {
                tcs.TrySetCanceled();
                Unsubscribe(progress);
            });

            return tcs.Task;
        }
    }
}