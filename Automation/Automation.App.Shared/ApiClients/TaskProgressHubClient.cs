using Automation.Shared.Data.Task;
using Microsoft.AspNetCore.SignalR.Client;

namespace Automation.App.Shared.ApiClients
{
    public class TaskProgressHubClient : IDisposable
    {
        private readonly HubConnection _connection;

        public event Action<TaskInstanceNotification>? NotificationReceived;
        public event Action<TaskInstanceState>? StateReceived;

        public TaskProgressHubClient(string hubUrl)
        {
            _connection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect()
                .Build();

            _connection.On<TaskInstanceNotification>("TaskInstanceNotification", (progress) =>
            {
                NotificationReceived?.Invoke(progress);
            });

            _connection.On<TaskInstanceState>("TaskIntanceState", (instanceState) =>
            {
                StateReceived?.Invoke(instanceState);
            });

            _connection.StartAsync();
        }

        public async void Dispose()
        {
            await _connection.StopAsync();
            await _connection.DisposeAsync();
        }
    }
}