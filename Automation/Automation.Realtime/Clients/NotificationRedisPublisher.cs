using Automation.Plugins.Shared;
using Automation.Realtime.Base;
using StackExchange.Redis;

namespace Automation.Realtime.Clients
{
    public class NotificationRedisPublisher : RedisSubscriber<TaskInstanceNotification>
    {
        public NotificationRedisPublisher(ConnectionMultiplexer connection) : base(
            connection,
            $"instances:notifications")
        {
        }

        public void Subscribe(Guid instanceId, IProgress<TaskInstanceNotification> callback)
        {
            base.Subscribe(new Progress<TaskInstanceNotification>((instanceProgress) =>
            {
                if (instanceProgress.InstanceId == instanceId)
                    callback.Report(instanceProgress);
            }));
        }
    }
}
