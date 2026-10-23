using System.Globalization;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Scoped;

namespace Fantoche.Worker.Storage;

/// <summary>
/// The storage of a worker : a <see cref="LocalStorage"/> of its own for each execution it runs,
/// <c>{folder}/{taskId}/{created}-{instanceId}</c>, the nodes of a workflow writing in the one of
/// its run. Only the last executions of each task keep theirs (see
/// <see cref="TaskSettings.StorageRetention"/>).
/// </summary>
public sealed class ExecutionStorages
{
    private readonly string _folder;

    /// <summary>The folders of the executions running, never removed.</summary>
    private readonly HashSet<string> _inUse = [];

    public ExecutionStorages(string folder)
    {
        _folder = folder;
    }

    /// <summary>
    /// The storage of [instance], an execution of [taskId], kept until <see cref="Release"/>.
    /// </summary>
    public LocalStorage Open(Guid taskId, TaskInstance instance)
    {
        string folder = FolderOf(taskId, instance);
        lock (_inUse)
            _inUse.Add(folder);
        return new LocalStorage(folder);
    }

    /// <summary>
    /// [instance] is over : remove the storage of the executions of [taskId] but the [keep] last
    /// ones and the ones still running.
    /// </summary>
    public void Release(Guid taskId, TaskInstance instance, int keep)
    {
        string taskFolder = Path.Combine(_folder, taskId.ToString("N"));
        List<string> removed;
        lock (_inUse)
        {
            _inUse.Remove(FolderOf(taskId, instance));
            if (!Directory.Exists(taskFolder))
                return;

            // Named after their creation, so the most recent come first.
            removed = [.. Directory.GetDirectories(taskFolder)
                .OrderByDescending(x => Path.GetFileName(x), StringComparer.Ordinal)
                .Skip(Math.Max(keep, 0))
                .Where(x => !_inUse.Contains(x))];
        }

        foreach (string folder in removed)
        {
            try
            {
                Directory.Delete(folder, true);
            }
            catch (IOException)
            {
                // Still read by something (an editor holding a file open) : the next release retries.
            }
        }
    }

    /// <summary>
    /// The disk space taken by the executions kept for each task or workflow, read off the folders :
    /// they are what holds the storage, whichever process wrote it.
    /// </summary>
    public List<StorageUsage> GetUsage()
    {
        if (!Directory.Exists(_folder))
            return [];

        List<StorageUsage> usages = [];
        foreach (string taskFolder in Directory.GetDirectories(_folder))
        {
            // Not a task folder : nothing of ours.
            if (!Guid.TryParseExact(Path.GetFileName(taskFolder), "N", out Guid taskId))
                continue;

            string[] executions = Directory.GetDirectories(taskFolder);
            long bytes = executions.Sum(SizeOf);
            usages.Add(new StorageUsage(taskId, executions.Length, bytes));
        }
        return usages;
    }

    /// <summary>
    /// A file can vanish while it is counted : the retention removes the folders of old executions
    /// at any time.
    /// </summary>
    private static long SizeOf(string folder)
    {
        long bytes = 0;
        try
        {
            foreach (string file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
                bytes += new FileInfo(file).Length;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
        return bytes;
    }

    private string FolderOf(Guid taskId, TaskInstance instance)
    {
        string created = instance.CreatedAt.ToUniversalTime().ToString("yyyyMMdd'T'HHmmssfffffff", CultureInfo.InvariantCulture);
        return Path.Combine(_folder, taskId.ToString("N"), $"{created}-{instance.Id:N}");
    }
}
