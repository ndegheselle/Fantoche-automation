using Automation.Realtime.Clients;
using Microsoft.AspNetCore.SignalR;

namespace Automation.Supervisor.Api.Hubs
{
    public class TaskProgressHub : Hub
    {
        private readonly RealtimeClients _realtime;
        public TaskProgressHub(RealtimeClients realtime)
        {
            _realtime = realtime;
        }


        public async Task SendtaskUpdate(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
    }
}