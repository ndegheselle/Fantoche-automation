using Automation.Realtime;
using Automation.Realtime.Clients;
using Microsoft.AspNetCore.SignalR;

namespace Automation.Supervisor.Api.Hubs
{
    public class TaskProgressHub : Hub
    {
        private readonly InstanceProgressRedisPublisher _progress;
        private readonly InstanceLifecycleRedisPublisher _lifecycle;

        public TaskProgressHub(RedisConnectionManager redis)
        {
            _lifecycle = new InstanceLifecycleRedisPublisher()
        }
    }
}