using Fantoche.Plugins.Shared;

namespace Fantoche.Shared.Remote;

/// <summary>
/// Where the hubs of the supervisor are reached.
/// </summary>
public static class SupervisorRoutes
{
    public const string Callers = "hubs/callers";
    public const string Workers = "hubs/workers";
}

/// <summary>
/// What a caller asks the supervisor. An execution is known by the id of its instance.
/// </summary>
public interface ICallerHub
{
    /// <summary>Queue [execution] for a worker, its outcome is sent back with <see cref="ICallerClient.Completed"/>.</summary>
    Task Execute(RemoteExecution execution);

    Task Cancel(Guid executionId);
}

/// <summary>
/// What the supervisor tells a caller about the executions it asked for.
/// </summary>
public interface ICallerClient
{
    /// <summary>An instance of the execution changed : the task itself or a node of the workflow.</summary>
    Task StateChanged(Guid executionId, RemoteInstance instance);

    Task Notified(Guid executionId, TaskNotification notification);

    /// <summary>The execution is over, [instance] is what it came to.</summary>
    Task Completed(RemoteInstance instance);
}

/// <summary>
/// What a worker tells the supervisor.
/// </summary>
public interface IWorkerHub
{
    /// <summary>The worker is ready to run up to [capacity] executions at once.</summary>
    Task Register(string workerId, int capacity);

    Task ReportState(Guid executionId, RemoteInstance instance);

    Task ReportNotification(Guid executionId, TaskNotification notification);

    Task Complete(RemoteInstance instance);
}

/// <summary>
/// What the supervisor asks a worker.
/// </summary>
public interface IWorkerClient
{
    Task Execute(RemoteExecution execution);

    Task Cancel(Guid executionId);
}
