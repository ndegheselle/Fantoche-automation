using System.Globalization;
using Fantoche.Shared.Data.Execution;

namespace Fantoche.Worker.Storage;

/// <summary>
/// The storage of the executions : a <see cref="LocalStorage"/> of its own for each of them,
/// <c>{folder}/{taskId}/{created}-{instanceId}</c>, the nodes of a workflow writing in the one of
/// its run. The folder only depends on the execution, so a resumed run finds its storage again
/// whichever worker sharing the folder resumes it.
/// </summary>
public sealed class ExecutionStorages
{
    private const string CreatedFormat = "yyyyMMdd'T'HHmmssfffffff";

    private readonly string _folder;

    public ExecutionStorages(string folder)
    {
        _folder = folder;
    }

    /// <summary>The storage of [instance], an execution of [taskId].</summary>
    public LocalStorage Open(Guid taskId, TaskInstance instance) => new(FolderOf(taskId, instance));

    /// <summary>
    /// Remove the storage of every execution of [folder] started before [cutoff]. Read from the
    /// names of the folders rather than from the history : the supervisor cleaning the history
    /// doesn't hold the folders of an application.
    /// </summary>
    public static void RemoveStartedBefore(string folder, DateTime cutoff)
    {
        if (!Directory.Exists(folder))
            return;

        IEnumerable<string> executions = Directory.GetDirectories(folder).SelectMany(Directory.GetDirectories);
        foreach (string execution in executions)
        {
            DateTime? created = CreatedOf(execution);
            if (created == null || created >= cutoff.ToUniversalTime())
                continue;

            try
            {
                Directory.Delete(execution, true);
            }
            catch (IOException)
            {
                // Still read by something (an editor holding a file open) : the next cleanup retries.
            }
        }
    }

    private string FolderOf(Guid taskId, TaskInstance instance)
    {
        string created = instance.CreatedAt.ToUniversalTime().ToString(CreatedFormat, CultureInfo.InvariantCulture);
        return Path.Combine(_folder, taskId.ToString("N"), $"{created}-{instance.Id:N}");
    }

    /// <summary>When the execution of [folder] started, null for a folder this didn't name.</summary>
    private static DateTime? CreatedOf(string folder)
    {
        string name = Path.GetFileName(folder);
        int separator = name.IndexOf('-');
        if (separator < 0)
            return null;

        bool isNamed = DateTime.TryParseExact(
            name[..separator],
            CreatedFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
            out DateTime created);
        return isNamed ? created : null;
    }
}
