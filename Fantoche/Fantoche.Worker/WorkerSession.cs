using System.Collections.Concurrent;
using Fantoche.Plugins.Shared;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Remote;
using Newtonsoft.Json.Linq;

namespace Fantoche.Worker;

/// <summary>
/// A <see cref="LocalWorker"/> as the supervisor knows it : it runs the executions the supervisor
/// dispatches and reports on them as they progress, whatever carries the messages (the hub of a
/// remote supervisor or a call to the one of the process).
/// </summary>
public sealed class WorkerSession : IWorkerClient
{
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

    /// <summary>
    /// What stops an execution : <see cref="Cancellation"/> at once, <see cref="Pause"/> once the
    /// nodes it runs are over. Not disposed : a cancel can still reach them once it is over, and
    /// they hold no timer.
    /// </summary>
    private sealed record Controls(CancellationTokenSource Cancellation, CancellationTokenSource Pause);

    private readonly LocalWorker _worker;
    private readonly IWorkerHub _supervisor;
    private readonly ConcurrentDictionary<Guid, Controls> _running = [];

    /// <param name="supervisor">What the executions are reported through.</param>
    public WorkerSession(LocalWorker worker, IWorkerHub supervisor)
    {
        _worker = worker;
        _supervisor = supervisor;
    }

    /// <summary>
    /// Run apart rather than awaited : the transport doesn't hand anything else over while it waits,
    /// a cancel included, and a supervisor of the same process would wait on the whole run. Its
    /// controls are in place before : a cancel or a pause sent right after it isn't lost.
    /// </summary>
    public Task Execute(RemoteExecution execution)
    {
        Controls controls = new(new CancellationTokenSource(), new CancellationTokenSource());
        _running[execution.Instance.Id] = controls;
        _ = Task.Run(() => RunAsync(execution, controls));
        return Task.CompletedTask;
    }

    public Task Cancel(Guid executionId)
    {
        if (_running.TryGetValue(executionId, out Controls? controls))
            controls.Cancellation.Cancel();
        return Task.CompletedTask;
    }

    public Task Pause(Guid executionId)
    {
        if (_running.TryGetValue(executionId, out Controls? controls))
            controls.Pause.Cancel();
        return Task.CompletedTask;
    }

    /// <summary>
    /// The supervisor suspends the executions of a worker it lost : running them is pointless, a
    /// resume runs them again wherever there is room.
    /// </summary>
    public void CancelAll()
    {
        foreach (Controls controls in _running.Values)
            controls.Cancellation.Cancel();
    }

    private async Task RunAsync(RemoteExecution execution, Controls controls)
    {
        Guid executionId = execution.Instance.Id;

        ThrottledNotifications notifications = new(x => _supervisor.ReportNotification(executionId, x));

        TaskInstance result;
        try
        {
            TaskInstancesProgress progress = new()
            {
                StateChanges = new StatesReporter(x => _supervisor.ReportState(executionId, x)),
                Notifications = notifications,
            };

            result = await _worker.ExecuteDispatchedAsync(execution, progress, controls.Cancellation.Token, controls.Pause.Token);
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
            // Only its own : a run resumed on this worker meanwhile has controls of its own.
            _running.TryRemove(KeyValuePair.Create(executionId, controls));
        }

        await notifications.FlushAsync();

        try
        {
            await _supervisor.Complete(result);
        }
        catch
        {
            // The connection was lost : the supervisor already failed the execution.
        }
    }
}
