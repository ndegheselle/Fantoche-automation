using Fantoche.Plugins.Shared;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Remote;
using Fantoche.Worker;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Fantoche.Services.Remote;

/// <summary>
/// A <see cref="LocalWorker"/> added to the workers of a remote supervisor, over its hub.
/// </summary>
public sealed class RemoteWorkerConnection : IWorkerHub, IAsyncDisposable
{
    /// <summary>
    /// A worker waits for the supervisor however long it takes to come back.
    /// </summary>
    private sealed class RetryForever : IRetryPolicy
    {
        public TimeSpan? NextRetryDelay(RetryContext retryContext) => TimeSpan.FromSeconds(5);
    }

    private readonly HubConnection _connection;
    private readonly WorkerSession _session;
    private readonly string _workerId;
    private readonly int _capacity;

    public RemoteWorkerConnection(Uri supervisor, LocalWorker worker, string workerId, int capacity)
    {
        _workerId = workerId;
        _capacity = capacity;
        _session = new WorkerSession(worker, this);

        _connection = new HubConnectionBuilder()
            .WithUrl(new Uri(supervisor, SupervisorRoutes.Workers))
            .WithAutomaticReconnect(new RetryForever())
            .AddJsonProtocol(options => FantocheJson.Configure(options.PayloadSerializerOptions))
            .Build();

        _connection.On<RemoteExecution>(nameof(IWorkerClient.Execute), _session.Execute);
        _connection.On<Guid>(nameof(IWorkerClient.Cancel), _session.Cancel);
        // The supervisor interrupts the executions of a connection it lost, to be resumed elsewhere.
        _connection.Reconnecting += _ =>
        {
            _session.CancelAll();
            return Task.CompletedTask;
        };
        _connection.Reconnected += _ => Register(_workerId, _capacity);
    }

    public async ValueTask DisposeAsync()
    {
        _session.CancelAll();
        await _connection.DisposeAsync();
    }

    /// <summary>
    /// Connect to the supervisor, waiting for it to be up, and register to it.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellation)
    {
        while (true)
        {
            try
            {
                await _connection.StartAsync(cancellation);
                break;
            }
            catch (HttpRequestException)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), cancellation);
            }
        }

        await Register(_workerId, _capacity);
    }

    public Task Register(string workerId, int capacity)
        => _connection.InvokeAsync(nameof(IWorkerHub.Register), workerId, capacity);

    public Task ReportState(Guid executionId, TaskInstance instance)
        => _connection.InvokeAsync(nameof(IWorkerHub.ReportState), executionId, instance);

    public Task ReportNotification(Guid executionId, TaskNotification notification)
        => _connection.SendAsync(nameof(IWorkerHub.ReportNotification), executionId, notification);

    public Task Complete(TaskInstance instance)
        => _connection.InvokeAsync(nameof(IWorkerHub.Complete), instance);
}
