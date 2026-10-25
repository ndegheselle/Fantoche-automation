namespace Fantoche.Shared.Remote;

/// <summary>An execution the supervisor holds, running on a worker or waiting for one.</summary>
public record ExecutionStatus(Guid ExecutionId, Guid TaskId, DateTime CreatedAt);

/// <summary>A worker connected to the supervisor and what it is running.</summary>
public record WorkerStatus(string Id, int Capacity, EnumWorkerRoles Roles, List<ExecutionStatus> Running);

/// <summary>
/// What the supervisor is dispatching right now : its workers and the executions waiting for one,
/// the first to be handed out coming first.
/// </summary>
public record DispatchStatus(List<WorkerStatus> Workers, List<ExecutionStatus> Pending);
