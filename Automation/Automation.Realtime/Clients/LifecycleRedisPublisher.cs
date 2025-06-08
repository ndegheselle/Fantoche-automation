using Automation.Plugins.Shared;
using Automation.Realtime.Base;
using StackExchange.Redis;

namespace Automation.Realtime.Clients
{
    public class TaskIntanceState
    {
        public Guid InstanceId { get; set; }
        public EnumTaskState State { get; set; }

        public TaskIntanceState(Guid instanceId, EnumTaskState state)
        {
            InstanceId = instanceId;
            State = state;
        }
    }

    public class LifecycleRedisPublisher : RedisSubscriber<TaskIntanceState>
    {
        public LifecycleRedisPublisher(ConnectionMultiplexer connection) : base(
            connection,
            $"instances:lifecycle")
        {}

        public void Subscribe(Guid instanceId, IProgress<EnumTaskState> callback)
        {
            base.Subscribe(new Progress<TaskIntanceState>((instanceState) =>
            {
                if (instanceState.InstanceId == instanceId)
                    callback.Report(instanceState.State);
            }));
        }

        // TODO : add a timeout for the wait state
        /// <summary>
        /// Wait for a specific state to be reached.
        /// </summary>
        /// <param name="targetState"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<EnumTaskState> WaitStateAsync(Guid instanceId, EnumTaskState targetState, CancellationToken? cancellationToken = null)
        {
            cancellationToken ??= CancellationToken.None;
            var tcs = new TaskCompletionSource<EnumTaskState>();

            var progress = new Progress<EnumTaskState>((state) =>
            {
                if (state.HasFlag(targetState))
                    tcs.TrySetResult(state);
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