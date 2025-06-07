using Automation.Plugins.Shared;
using Automation.Realtime.Base;
using StackExchange.Redis;

namespace Automation.Realtime.Clients
{
    public class TaskInstanceProgress
    {
        public Guid InstanceId { get; set; }
        public TaskProgress Progress { get; set; }

        public TaskInstanceProgress(Guid instanceId, TaskProgress progress)
        {
            InstanceId = instanceId;
            Progress = progress;
        }
    }

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

    public class InstanceProgressRedisPublisher : RedisSubscriber<TaskInstanceProgress>
    {
        public InstanceProgressRedisPublisher(ConnectionMultiplexer connection, Guid instanceId) : base(
            connection,
            $"instances:progress:{instanceId}")
        {
        }
    }

    public 

    public class InstanceLifecycleRedisPublisher : RedisSubscriber<TaskIntanceState>
    {
        public InstanceLifecycleRedisPublisher(ConnectionMultiplexer connection, Guid instanceId) : base(
            connection,
            $"instances:lifecycle:{instanceId}")
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

            var progress = new Progress<TaskIntanceState>((instanceState) =>
            {
                if (instanceState.State.HasFlag(targetState))
                {
                    tcs.TrySetResult(instanceState.State);
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