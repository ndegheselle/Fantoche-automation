using System.Collections.Concurrent;
using Fantoche.Plugins.Shared;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Remote;
using Microsoft.AspNetCore.SignalR.Client;

namespace Fantoche.Worker.Executor.Remote;

/// <summary>
/// A worker as the supervisor knows it : it runs the executions the supervisor sends and reports
/// on them as they progress.
/// </summary>
public sealed class WorkerConnection : IAsyncDisposable
{
    /// <summary>
    /// A worker waits for the supervisor however long it takes to come back.
    /// </summary>
    private sealed class RetryForever : IRetryPolicy
    {
        public TimeSpan? NextRetryDelay(RetryContext retryContext) => TimeSpan.FromSeconds(5);
    }

    /// <summary>
    /// Reports synchronously : the states of an instance have to leave in the order they happened.
    /// </summary>
    private sealed class Reporter<T> : IProgress<T>
    {
        private readonly Func<T, Task> _report;
        public Reporter(Func<T, Task> report) => _report = report;
        public void Report(T value) => _report(value).GetAwaiter().GetResult();
    }

    private readonly HubConnection _connection;
    private readonly string _workerId;
    private readonly int _capacity;
    private readonly ITaskExecutor _executor;
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _running = [];

    public WorkerConnection(Uri supervisor, string workerId, int capacity, ITaskExecutor executor)
    {
        _workerId = workerId;
        _capacity = capacity;
        _executor = executor;

        _connection = new HubConnectionBuilder()
            .WithUrl(new Uri(supervisor, SupervisorRoutes.Workers))
            .WithAutomaticReconnect(new RetryForever())
            .Build();

        // Not awaited : the connection doesn't receive anything else while a handler runs.
        _connection.On<RemoteExecution>(nameof(IWorkerClient.Execute), execution => { _ = RunAsync(execution); });
        _connection.On<Guid>(nameof(IWorkerClient.Cancel), Cancel);
        // The supervisor fails the executions of a connection it lost : running them is pointless.
        _connection.Reconnecting += _ =>
        {
            CancelAll();
            return Task.CompletedTask;
        };
        _connection.Reconnected += _ => RegisterAsync();
    }

    public async ValueTask DisposeAsync()
    {
        CancelAll();
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

        await RegisterAsync();
    }

    public Task StopAsync() => _connection.StopAsync();

    private Task RegisterAsync()
        => _connection.InvokeAsync(nameof(IWorkerHub.Register), _workerId, _capacity);

    private async Task RunAsync(RemoteExecution execution)
    {
        Guid executionId = execution.Instance.Id;
        // Not disposed : a cancel can still reach it once it is over, and it holds no timer.
        CancellationTokenSource cancellation = new();
        _running[executionId] = cancellation;

        RemoteInstance result;
        try
        {
            var (task, instance) = execution.Unpack();
            TaskInstancesProgress progress = new()
            {
                StateChanges = new Reporter<TaskInstance>(x =>
                    _connection.InvokeAsync(nameof(IWorkerHub.ReportState), executionId, RemoteInstance.From(x))),
                Notifications = new Reporter<TaskNotification>(x =>
                    _connection.InvokeAsync(nameof(IWorkerHub.ReportNotification), executionId, x)),
            };

            instance = await _executor.ExecuteAsync(task, instance, progress, cancellation.Token);
            result = RemoteInstance.From(instance);
        }
        catch (Exception ex)
        {
            // The executor reports the failures of the task itself, this is about setting it up.
            result = execution.Instance.Finished(EnumTaskState.Failed, ex.ToString());
        }
        finally
        {
            _running.TryRemove(executionId, out _);
        }

        try
        {
            await _connection.InvokeAsync(nameof(IWorkerHub.Complete), result);
        }
        catch
        {
            // The connection was lost : the supervisor already failed the execution.
        }
    }

    private void Cancel(Guid executionId)
    {
        if (_running.TryGetValue(executionId, out var cancellation))
            cancellation.Cancel();
    }

    private void CancelAll()
    {
        foreach (CancellationTokenSource cancellation in _running.Values)
            cancellation.Cancel();
    }
}
