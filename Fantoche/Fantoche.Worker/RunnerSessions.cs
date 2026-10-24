using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Services;

namespace Fantoche.Worker;

/// <summary>
/// The processes running executions out of a same folder : the copies of the application on a
/// machine, or the supervisors of a database. Each one holds a lock file for as long as it lives,
/// which is how the others tell whether a run it started is still running or was left behind by a
/// process that is gone.
/// </summary>
public sealed class RunnerSessions : IDisposable
{
    private readonly string _folder;
    private readonly FileStream _lock;

    /// <summary>The session of this process, what its runs are marked with.</summary>
    public string CurrentId { get; } = Guid.NewGuid().ToString("N");

    /// <param name="folder">Where the sessions sharing the runs keep their lock file.</param>
    public RunnerSessions(string folder)
    {
        _folder = folder;
        Directory.CreateDirectory(folder);
        // Removed on a clean exit, a crash leaving it for IsAlive to find unlocked.
        _lock = new FileStream(PathOf(CurrentId), FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.DeleteOnClose);
    }

    public void Dispose() => _lock.Dispose();

    /// <summary>
    /// Whether the session [sessionId] still runs : its lock file is held. A file nobody holds is
    /// what a crash leaves, it is removed on the way.
    /// </summary>
    public bool IsAlive(string? sessionId)
    {
        if (sessionId == null)
            return false;

        string path = PathOf(sessionId);
        if (!File.Exists(path))
            return false;

        try
        {
            using (new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
            }
        }
        catch (FileNotFoundException)
        {
            return false;
        }
        catch (IOException)
        {
            // Held by the process it stands for.
            return true;
        }

        File.Delete(path);
        return false;
    }

    /// <summary>
    /// Mark the runs of [applicationId] (null for the ones the supervisor dispatches) whose process
    /// is gone as interrupted, so they can be resumed. The ones a living session runs are left alone.
    /// </summary>
    public async Task InterruptLeftRunsAsync(IHistoryService history, string? applicationId)
    {
        IReadOnlyList<TaskInstance> unfinished = await history.GetUnfinishedRunsAsync(applicationId);
        IEnumerable<TaskInstance> left = unfinished.Where(x => !IsAlive(x.SessionId));
        foreach (TaskInstance run in left)
            await history.InterruptRunAsync(run.Id);
    }

    private string PathOf(string sessionId) => Path.Combine(_folder, $"{sessionId}.lock");
}
