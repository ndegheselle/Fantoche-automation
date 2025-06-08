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

    public class ProgressRedisPublisher : RedisSubscriber<TaskInstanceProgress>
    {
        public ProgressRedisPublisher(ConnectionMultiplexer connection) : base(
            connection,
            $"instances:progress")
        {
        }

        public void Subscribe(Guid instanceId, IProgress<TaskProgress> callback)
        {
            base.Subscribe(new Progress<TaskInstanceProgress>((instanceProgress) =>
            {
                if (instanceProgress.InstanceId == instanceId)
                    callback.Report(instanceProgress.Progress);
            }));
        }

    }
}
