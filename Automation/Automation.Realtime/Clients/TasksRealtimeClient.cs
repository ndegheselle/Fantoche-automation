using Automation.Plugins.Shared;
using Automation.Realtime.Base;
using Automation.Shared.Data;
using StackExchange.Redis;
using System.Threading;

namespace Automation.Realtime.Clients
{
    public class ProgressRedisPublisher : RedisPublisher<TaskProgress>
    {
        public ProgressRedisPublisher(ConnectionMultiplexer connection, Guid instanceId) : base(
            connection,
            $"tasks:instance:{instanceId}:progress")
        {
        }
    }

    public class LifecycleRedisPublisher : RedisPublisher<EnumTaskState>
    {
        public LifecycleRedisPublisher(ConnectionMultiplexer connection, Guid instanceId) : base(
            connection,
            $"tasks:instance:{instanceId}:lifecycle")
        {
        }

        public Task WaitStateAsync(EnumTaskState targetState, CancellationToken? cancellationToken)
        {
            cancellationToken ??= CancellationToken.None;
            var tcs = new TaskCompletionSource();

            void OnStateChanged(EnumTaskState state)
            {
                if (state == targetState)
                {
                    tcs.TrySetResult();
                    Unsubscribe();
                }
                else if (state == EnumTaskState.Failed)
                {
                    tcs.TrySetException(new Exception("Task failed"));
                    Unsubscribe();
                }
            }

            Subscribe(OnStateChanged);

            cancellationToken?.Register(() =>
            {
                tcs.TrySetCanceled();
                Unsubscribe();
            });

            return tcs.Task;
        }
    }

    public class TasksRealtimeClient
    {
        public ProgressRedisPublisher Progress { get; private set; }

        public LifecycleRedisPublisher Lifecycle { get; private set; }

        public TasksRealtimeClient(ConnectionMultiplexer connection, Guid instanceId)
        {
            Progress = new ProgressRedisPublisher(connection, instanceId);
            Lifecycle = new LifecycleRedisPublisher(connection, instanceId);
        }
    }
}