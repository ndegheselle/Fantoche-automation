using System.Collections.Concurrent;
using Fantoche.Plugins.Shared;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Remote;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

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
    /// Reports synchronously : the states of an instance have to leave in the order they happened,
    /// and before the executor changes the instance again.
    /// </summary>
    private sealed class StatesReporter : IProgress<TaskInstance>
    {
        private readonly Func<TaskInstance, Task> _report;
        public StatesReporter(Func<TaskInstance, Task> report) => _report = report;
        public void Report(TaskInstance value) => _report(value).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Keeps the last notification and sends it at most every <see cref="Interval"/>, without holding
    /// the task : a task may notify on every row it handles, which would flood the supervisor.
    /// </summary>
    private sealed class ThrottledNotifications : IProgress<TaskNotification>
    {
        private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(200);

        private readonly Func<TaskNotification, Task> _send;
        private readonly object _lock = new();
        private TaskNotification? _pending;
        private bool _isScheduled;

        public ThrottledNotifications(Func<TaskNotification, Task> send) => _send = send;

        public void Report(TaskNotification value)
        {
            lock (_lock)
            {
                _pending = value;
                if (_isScheduled)
                    return;
                _isScheduled = true;
            }
            _ = SendLaterAsync();
        }

        /// <summary>
        /// Send what is pending right away, so the last notification of a task isn't lost.
        /// </summary>
        public async Task FlushAsync()
        {
            TaskNotification? pending;
            lock (_lock)
            {
                pending = _pending;
                _pending = null;
                _isScheduled = false;
            }

            if (pending == null)
                return;

            try
            {
                await _send(pending.Value);
            }
            catch
            {
                // A notification only informs : losing it to a lost connection breaks nothing.
            }
        }

        private async Task SendLaterAsync()
        {
            await Task.Delay(Interval);
            await FlushAsync();
        }
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
            .AddJsonProtocol(options => FantocheJson.Configure(options.PayloadSerializerOptions))
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

        ThrottledNotifications notifications = new(x =>
            _connection.SendAsync(nameof(IWorkerHub.ReportNotification), executionId, x));

        TaskInstance result;
        try
        {
            var (task, instance) = execution.Unpack();
            TaskInstancesProgress progress = new()
            {
                StateChanges = new StatesReporter(x =>
                    _connection.InvokeAsync(nameof(IWorkerHub.ReportState), executionId, x)),
                Notifications = notifications,
            };

            result = await _executor.ExecuteAsync(task, instance, progress, cancellation.Token);
        }
        catch (Exception ex)
        {
            // The executor reports the failures of the task itself, this is about setting it up.
            result = execution.Instance;
            result.State = EnumTaskState.Failed;
            result.Output = JToken.FromObject(ex.ToString());
        }
        finally
        {
            _running.TryRemove(executionId, out _);
        }

        await notifications.FlushAsync();

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
