namespace Automation.Plugins.Shared;

/// <summary>
/// Capabilities the engine hands to a task at execution time.
/// Plugins use this to surface progress, choose which output branches to follow,
/// and (eventually) reach other framework services without taking a direct
/// dependency on the engine's internals.
/// </summary>
public interface ITaskRuntime
{
    /// <summary>
    /// Progress sink — notifications are forwarded to the executor's progress listener.
    /// </summary>
    IProgress<TaskNotification>? Progress { get; }

    /// <summary>
    /// Output branches declared by the running task (see <see cref="ITask.OutputBranches"/>).
    /// Empty when the task uses the default single output.
    /// </summary>
    IReadOnlyList<string> OutputBranches { get; }

    /// <summary>
    /// Restrict downstream propagation to the named output branches. Can be called
    /// multiple times; the executor unions the names. When never called (or called
    /// with no names), every output branch propagates as usual.
    /// Unknown names are ignored — the engine validates against <see cref="OutputBranches"/>.
    /// </summary>
    void ActivateOutputs(params string[] names);
}
