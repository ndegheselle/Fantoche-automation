using System.Collections.Concurrent;
using Fantoche.Plugins.Shared;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Scoped;
using Fantoche.Shared.Remote;
using Newtonsoft.Json.Linq;

namespace Fantoche.Worker.Executor;

/// <summary>
/// Run the tasks on the workers of the supervisor. Only the instance is sent : the supervisor reads
/// the task from its database, and stores what the worker reports along the way.
/// </summary>
public sealed class RemoteTaskExecutor : ITaskExecutor, ICallerClient
{
    private sealed record PendingExecution(TaskInstancesProgress? Progress, TaskCompletionSource<TaskInstance> Completion);

    private readonly ICallerHub _supervisor;
    private readonly ConcurrentDictionary<Guid, PendingExecution> _pending = [];

    public RemoteTaskExecutor(ISupervisor supervisor)
    {
        _supervisor = supervisor.ConnectCaller(this);
    }

    public async Task<TaskInstance> ExecuteAsync(
        BaseAutomationTask task,
        TaskInstance instance,
        TaskInstancesProgress? progress = null,
        CancellationToken? cancellation = null)
    {
        PendingExecution pending = new(progress, new TaskCompletionSource<TaskInstance>(TaskCreationOptions.RunContinuationsAsynchronously));
        _pending[instance.Id] = pending;
        try
        {
            await _supervisor.Execute(instance);

            // The worker is the one stopping the task : the outcome still comes back as a completion.
            using var registration = cancellation?.Register(() => _ = _supervisor.Cancel(instance.Id));
            TaskInstance result = await pending.Completion.Task;

            instance.State = result.State;
            instance.Output = result.Output;
            // Set after the state, which stamps its own.
            instance.FinishedAt = result.FinishedAt;
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

    Task ICallerClient.Notified(Guid executionId, TaskNotification notification)
    {
        if (_pending.TryGetValue(executionId, out var pending))
            pending.Progress?.Notifications?.Report(notification);
        return Task.CompletedTask;
    }

    Task ICallerClient.Completed(TaskInstance instance)
    {
        if (_pending.TryGetValue(instance.Id, out var pending))
            pending.Completion.TrySetResult(instance);
        return Task.CompletedTask;
    }
}
