using System.Collections.Concurrent;
using Fantoche.Plugins.Shared;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Scoped;
using Fantoche.Shared.Remote;
using Fantoche.Worker.Executor;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json.Linq;

namespace Fantoche.Worker.Remote;

/// <summary>
/// Run the tasks on the workers of a supervisor. What the worker reports along the way is relayed
/// to the progress of the caller, as if the task ran here.
/// </summary>
public sealed class RemoteTaskExecutor : ITaskExecutor, IAsyncDisposable
{
    private sealed record PendingExecution(TaskInstancesProgress? Progress, TaskCompletionSource<RemoteInstance> Completion);

    private readonly HubConnection _connection;
    private readonly SemaphoreSlim _connecting = new(1, 1);
    private readonly ConcurrentDictionary<Guid, PendingExecution> _pending = [];

    public RemoteTaskExecutor(Uri supervisor)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(new Uri(supervisor, SupervisorRoutes.Callers))
            .Build();

        _connection.On<Guid, RemoteInstance>(nameof(ICallerClient.StateChanged), OnStateChanged);
        _connection.On<Guid, TaskNotification>(nameof(ICallerClient.Notified), OnNotified);
        _connection.On<RemoteInstance>(nameof(ICallerClient.Completed), OnCompleted);
        _connection.Closed += OnClosed;
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
        _connecting.Dispose();
    }

    public async Task<TaskInstance> ExecuteAsync(
        BaseAutomationTask task,
        TaskInstance instance,
        TaskInstancesProgress? progress = null,
        CancellationToken? cancellation = null)
    {
        PendingExecution pending = new(progress, new TaskCompletionSource<RemoteInstance>(TaskCreationOptions.RunContinuationsAsynchronously));
        _pending[instance.Id] = pending;
        try
        {
            RemoteExecution execution = RemoteExecution.From(task, instance);
            await ConnectAsync();
            await _connection.InvokeAsync(nameof(ICallerHub.Execute), execution);

            // The worker is the one stopping the task : the outcome still comes back as a completion.
            using var registration = cancellation?.Register(() => _ = CancelAsync(instance.Id));
            RemoteInstance result = await pending.Completion.Task;
            result.ApplyResultTo(instance);
        }
        catch (Exception ex)
        {
            instance.State = EnumTaskState.Failed;
            instance.Output = JToken.FromObject(ex.ToString());
        }
        finally
        {
            _pending.TryRemove(instance.Id, out _);
        }

        return instance;
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

    private async Task CancelAsync(Guid executionId)
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

    private void OnStateChanged(Guid executionId, RemoteInstance instance)
    {
        if (_pending.TryGetValue(executionId, out var pending))
            pending.Progress?.StateChanges?.Report(instance.ToInstance());
    }

    private void OnNotified(Guid executionId, TaskNotification notification)
    {
        if (_pending.TryGetValue(executionId, out var pending))
            pending.Progress?.Notifications?.Report(notification);
    }

    private void OnCompleted(RemoteInstance instance)
    {
        if (_pending.TryGetValue(instance.Id, out var pending))
            pending.Completion.TrySetResult(instance);
    }

    /// <summary>
    /// The supervisor forgets the executions of a connection it lost : none of them will complete.
    /// </summary>
    private Task OnClosed(Exception? error)
    {
        foreach (PendingExecution pending in _pending.Values)
            pending.Completion.TrySetException(new ExecutionException($"The connection to the supervisor was lost. {error?.Message}"));
        return Task.CompletedTask;
    }
}
