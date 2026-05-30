using Microsoft.AspNetCore.SignalR.Client;

namespace Automation.App.Shared.ApiClients
{
    // MIGRATION STUB: the original TaskProgressHubClient exposed typed events
    // (TaskInstanceNotification / TaskInstanceState) from the now-removed
    // "Automation.Shared.Data.Task" namespace. Those typed handlers are stubbed out pending the
    // rework against Automation.Worker + SQLite. Connection lifecycle is preserved.
    // Original implementation: see git history (pre-'avalonia-migration').
    public class TaskProgressHubClient : IDisposable
    {
        private readonly HubConnection _connection;

        public TaskProgressHubClient(string hubUrl)
        {
            _connection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect()
                .Build();

            // TODO (rework): re-wire TaskInstanceNotification / TaskInstanceState handlers once
            // the realtime contract is re-established against the worker.

            _connection.StartAsync();
        }

        public async void Dispose()
        {
            await _connection.StopAsync();
            await _connection.DisposeAsync();
        }
    }
}
