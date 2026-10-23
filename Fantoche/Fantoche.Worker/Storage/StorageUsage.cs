namespace Fantoche.Worker.Storage;

/// <summary>The storage kept by a worker for the executions of a task or workflow.</summary>
/// <param name="Executions">How many executions still have their folder.</param>
public record StorageUsage(Guid TaskId, int Executions, long Bytes);
