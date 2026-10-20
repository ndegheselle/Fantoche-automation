namespace Fantoche.Shared.Data.Execution;

/// <summary>
/// The run the files and tables of the tasks are kept under, in the storage : the workflow or the
/// task started on its own, and the instance of it. What a run takes on disk is then the size of
/// its folder, and what a workflow or a task takes the size of the folders of its runs.
/// </summary>
public record StorageOwner(Guid TaskId, Guid InstanceId);
