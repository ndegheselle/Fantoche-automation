using Fantoche.Plugins.Shared;
using Fantoche.Shared.Data.Execution;

namespace Fantoche.Shared.Remote;

/// <summary>
/// Where the hubs of the supervisor are reached. Its data is read and written through its HTTP API,
/// the hubs only carry what it pushes.
/// </summary>
public static class SupervisorRoutes
{
    public const string Callers = "hubs/callers";
    public const string Workers = "hubs/workers";
    public const string History = "hubs/history";
    public const string Dispatch = "hubs/dispatch";
}

/// <summary>
/// What a caller asks the supervisor. An execution is known by the id of its instance.
/// </summary>
public interface ICallerHub
{
    /// <summary>
    /// Queue [instance] for a worker, the supervisor reading its task from the database. Its outcome
    /// is sent back with <see cref="ICallerClient.Completed"/>.
    /// </summary>
    Task Execute(TaskInstance instance);

    Task Cancel(Guid executionId);
}

/// <summary>
/// What the supervisor tells a caller about the executions it asked for. Their states are stored by
/// the supervisor and pushed with <see cref="IHistoryClient"/>, like any other.
/// </summary>
public interface ICallerClient
{
    Task Notified(Guid executionId, TaskNotification notification);

    /// <summary>The execution is over, [instance] is what it came to.</summary>
    Task Completed(TaskInstance instance);
}

/// <summary>
/// What the supervisor pushes to every application : the instances it stores, as they change.
/// </summary>
public interface IHistoryClient
{
    Task InstanceAdded(TaskInstance instance);

    Task InstanceUpdated(TaskInstance instance);
}

/// <summary>
/// What the supervisor pushes to every application : what it dispatches, as it changes.
/// </summary>
public interface IDispatchClient
{
    Task StatusChanged(DispatchStatus status);
}

/// <summary>
/// What a worker tells the supervisor.
/// </summary>
public interface IWorkerHub
{
    /// <summary>The worker is ready to run up to [capacity] executions at once.</summary>
    Task Register(string workerId, int capacity);

    /// <summary>An instance of the execution changed : the task itself or a node of the workflow.</summary>
    Task ReportState(Guid executionId, TaskInstance instance);

    Task ReportNotification(Guid executionId, TaskNotification notification);

    Task Complete(TaskInstance instance);
}

/// <summary>
/// What the supervisor asks a worker.
/// </summary>
public interface IWorkerClient
{
    Task Execute(RemoteExecution execution);

    Task Cancel(Guid executionId);
}
