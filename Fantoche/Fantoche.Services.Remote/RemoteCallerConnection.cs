using System.Collections.Concurrent;
using Fantoche.Plugins.Shared;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Remote;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

namespace Fantoche.Services.Remote;

/// <summary>
/// The hub of the supervisor a caller sends its executions through, connected the first time one
/// is sent. A lost connection fails the executions it carried : the supervisor forgets them along
/// with the connection.
/// </summary>
internal sealed class RemoteCallerConnection : ICallerHub, IAsyncDisposable
{
    private readonly HubConnection _connection;
    private readonly SemaphoreSlim _connecting = new(1, 1);
    private readonly ICallerClient _caller;

    /// <summary>The executions sent and not completed yet.</summary>
    private readonly ConcurrentDictionary<Guid, TaskInstance> _carried = [];

    public RemoteCallerConnection(Uri supervisor, ICallerClient caller)
    {
        _caller = caller;
        _connection = new HubConnectionBuilder()
            .WithUrl(new Uri(supervisor, SupervisorRoutes.Callers))
            .AddJsonProtocol(options => FantocheJson.Configure(options.PayloadSerializerOptions))
            .Build();

        _connection.On<Guid, TaskNotification>(nameof(ICallerClient.Notified), _caller.Notified);
        _connection.On<TaskInstance>(nameof(ICallerClient.Completed), OnCompleted);
        _connection.Closed += OnClosed;
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
        _connecting.Dispose();
    }

    public async Task Execute(TaskInstance instance, JToken? globalContext)
    {
        _carried[instance.Id] = instance;
        try
        {
            await ConnectAsync();
            await _connection.InvokeAsync(nameof(ICallerHub.Execute), instance, globalContext);
        }
        catch
        {
            _carried.TryRemove(instance.Id, out _);
            throw;
        }
    }

    public async Task Cancel(Guid executionId)
    {
        try
        {
            await _connection.InvokeAsync(nameof(ICallerHub.Cancel), executionId);
        }
        catch
        {
            // Nothing to cancel through a lost connection : losing it fails the execution already.
        }
    }

    private async Task ConnectAsync()
    {
        await _connecting.WaitAsync();
        try
        {
            if (_connection.State == HubConnectionState.Disconnected)
                await _connection.StartAsync();
        }
        finally
        {
            _connecting.Release();
        }
    }

    private Task OnCompleted(TaskInstance instance)
    {
        _carried.TryRemove(instance.Id, out _);
        return _caller.Completed(instance);
    }

    private async Task OnClosed(Exception? error)
    {
        foreach (Guid executionId in _carried.Keys)
        {
            if (!_carried.TryRemove(executionId, out TaskInstance? instance))
                continue;

            TaskInstance lost = FantocheJson.Copy(instance);
            lost.State = EnumTaskState.Failed;
            lost.Output = JToken.FromObject($"The connection to the supervisor was lost. {error?.Message}");
            await _caller.Completed(lost);
        }
    }
}
