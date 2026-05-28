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
    /// Deactivate the output (even if the task is completed it )
    /// </summary>
    public void DeactivateOutput(bool deactivate = true);
}
