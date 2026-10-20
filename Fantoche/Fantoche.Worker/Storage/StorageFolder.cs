using Fantoche.Shared.Data.Execution;

namespace Fantoche.Worker.Storage;

/// <summary>
/// What the storage holds of a workflow or a task : the folder of the task, and the runs it holds.
/// </summary>
/// <param name="TaskId">Null for a folder that isn't the one of a task.</param>
/// <param name="Folder">The name of the folder, the id of the task for the folder of one.</param>
public record TaskStorageUsage(Guid? TaskId, string Folder, int Runs, long Bytes);

/// <summary>
/// The folder of the storage, every run keeping what its tasks save in a folder of its own (see
/// <see cref="StorageOwner"/>) :
/// <code>
/// {task id}/{instance id}/tables/{table}.parquet
/// {task id}/{instance id}/files/{file}/{name}
/// </code>
/// </summary>
public sealed class StorageFolder
{
    public string Path { get; }

    public StorageFolder(string path)
    {
        Path = path;
    }

    /// <summary>The storage the tasks of the run [owner] are given.</summary>
    public LocalStorage For(StorageOwner owner) => new(this, owner);

    public string PathOf(StorageOwner owner) => System.IO.Path.Combine(Path, $"{owner.TaskId:N}", $"{owner.InstanceId:N}");

    /// <summary>
    /// What each workflow or task takes, the largest first. Measured on what the disk holds now :
    /// the runs keep writing while the folder is walked.
    /// </summary>
    public IReadOnlyList<TaskStorageUsage> MeasureUsage()
    {
        if (!Directory.Exists(Path))
            return [];

        EnumerationOptions everything = new() { RecurseSubdirectories = true, IgnoreInaccessible = true };
        List<TaskStorageUsage> usages = [];
        foreach (DirectoryInfo taskFolder in new DirectoryInfo(Path).EnumerateDirectories())
        {
            Guid? taskId = Guid.TryParseExact(taskFolder.Name, "N", out Guid id) ? id : null;
            int runs = taskFolder.EnumerateDirectories().Count();
            long bytes = taskFolder.EnumerateFiles("*", everything).Sum(x => x.Length);
            usages.Add(new TaskStorageUsage(taskId, taskFolder.Name, runs, bytes));
        }

        return [.. usages.OrderByDescending(x => x.Bytes)];
    }
}
