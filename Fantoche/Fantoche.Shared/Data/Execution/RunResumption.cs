using Fantoche.Shared.Data.Scoped;
using Fantoche.Shared.Services;

namespace Fantoche.Shared.Data.Execution;

/// <summary>
/// When a run can be resumed : the rules are the same wherever it is resumed (the application for
/// the runs it started, the supervisor for the ones it dispatched).
/// </summary>
public static class RunResumption
{
    /// <summary>
    /// Throws when [run] can't be resumed by [applicationId] (null for the supervisor) on [task] as
    /// it is now, the graph of a workflow having to be loaded.
    /// </summary>
    /// <exception cref="ExecutionException">The reason the run can't be resumed.</exception>
    public static void EnsureResumable(TaskInstance run, BaseAutomationTask task, string? applicationId)
    {
        if (run.ParentInstanceId != null)
            throw new ExecutionException("Only a whole run is resumed, not one of its nodes.");

        if (run.ApplicationId != applicationId)
        {
            throw new ExecutionException(run.ApplicationId == null
                ? "The run was dispatched to the workers : only the supervisor resumes it."
                : $"The run was started by the application '{run.ApplicationId}' : only it resumes it.");
        }

        if (run.State is not (EnumTaskState.Interrupted or EnumTaskState.Paused or EnumTaskState.AwaitingUser))
            throw new ExecutionException($"Only an interrupted, paused or answered run is resumed, this one is {run.State}.");

        // The run is walked again on the version it ran, but the nested workflows and the tasks are
        // read as they are now : one that changed would meet nodes that are gone, or skip new ones.
        if (run.DefinitionHash != RunDefinition.HashOf(task))
            throw new ExecutionException("A nested workflow or a task of the run changed since it started : it can't be resumed.");
    }
}
