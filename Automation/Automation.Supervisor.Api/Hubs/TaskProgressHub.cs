using Automation.Plugins.Shared;
using Automation.Realtime.Clients;
using Microsoft.AspNetCore.SignalR;

namespace Automation.Supervisor.Api.Hubs
{
    public class TaskProgressHub : Hub, IDisposable
    {
        private readonly RealtimeClients _realtime;

        private readonly Progress<TaskInstanceNotification> _onProgress;
        private readonly Progress<TaskIntanceState> _onLifecycle;

        public TaskProgressHub(RealtimeClients realtime)
        {
            _realtime = realtime;

            _onProgress = new Progress<TaskInstanceNotification>((progress) =>
            {
                _ = SendTaskInstanceNotification(progress);
            });

            _onLifecycle = new Progress<TaskIntanceState>((instanceState) =>
            {
                _ = SendTaskInstanceStateUpdate(instanceState);
            });

            _realtime.Notifications.Subscribe(_onProgress);
            _realtime.Lifecycle.Subscribe(_onLifecycle);
        }

        public new void Dispose()
        {
            base.Dispose();
            _realtime.Notifications.Unsubscribe(_onProgress);
            _realtime.Lifecycle.Unsubscribe(_onLifecycle);
        }

        public async Task SendTaskInstanceStateUpdate(TaskIntanceState instanceState)
        {
            await Clients.All.SendAsync("TaskIntanceState", instanceState);
        }

        public async Task SendTaskInstanceNotification(TaskInstanceNotification instanceProgress)
        {
            await Clients.All.SendAsync("TaskInstanceNotification", instanceProgress);
        }
    }
}