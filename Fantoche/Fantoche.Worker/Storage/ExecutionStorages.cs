using System.Globalization;
using Fantoche.Shared.Data.Execution;

namespace Fantoche.Worker.Storage;

/// <summary>
/// The storage of the executions : a <see cref="LocalStorage"/> of its own for each of them,
/// <c>{folder}/{taskId}/{created}-{instanceId}</c>, the nodes of a workflow writing in the one of
/// its run. The folder only depends on the execution, so a resumed run finds its storage again
/// whichever worker sharing the folder resumes it, and the cleanup of a run finds its folder.
/// </summary>
public sealed class ExecutionStorages
{
    private readonly string _folder;

    public ExecutionStorages(string folder)
    {
        _folder = folder;
    }

    /// <summary>The storage of [instance], an execution of [taskId].</summary>
    public LocalStorage Open(Guid taskId, TaskInstance instance) => new(FolderOf(taskId, instance));

    /// <summary>Remove the storage of [run], an execution of the task it points at.</summary>
    public void Remove(TaskInstance run) => TryDelete(FolderOf(run.TaskId, run));

    /// <summary>
    /// Remove the storage of every execution not among [runIds] : the runs the history doesn't hold
    /// anymore. A folder this didn't name is left alone.
    /// </summary>
    public void RemoveAllBut(IReadOnlySet<Guid> runIds)
    {
        if (!Directory.Exists(_folder))
            return;

        IEnumerable<string> executions = Directory.GetDirectories(_folder).SelectMany(Directory.GetDirectories);
        foreach (string execution in executions)
        {
            if (RunIdOf(execution) is Guid runId && !runIds.Contains(runId))
                TryDelete(execution);
        }
    }

    private string FolderOf(Guid taskId, TaskInstance instance)
    {
        string created = instance.CreatedAt.ToUniversalTime().ToString("yyyyMMdd'T'HHmmssfffffff", CultureInfo.InvariantCulture);
        return Path.Combine(_folder, taskId.ToString("N"), $"{created}-{instance.Id:N}");
    }

    /// <summary>The run whose storage [folder] is, null for a folder this didn't name.</summary>
    private static Guid? RunIdOf(string folder)
    {
        string name = Path.GetFileName(folder);
        int separator = name.LastIndexOf('-');
        if (separator < 0 || !Guid.TryParseExact(name[(separator + 1)..], "N", out Guid runId))
            return null;
        return runId;
    }

    private static void TryDelete(string folder)
    {
        try
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
        }
        catch (IOException)
        {
            // Still read by something (an editor holding a file open) : left as it is.
        }
    }
}
