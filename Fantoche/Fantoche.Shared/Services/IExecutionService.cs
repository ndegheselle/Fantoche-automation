using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Scoped;
using Newtonsoft.Json.Linq;

namespace Fantoche.Shared.Services;

public class ExecutionException : Exception
{
    public ExecutionException(string message) : base(message) { }
    public ExecutionException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// Start and follow the executions of the tasks and workflows.
/// </summary>
public interface IExecutionService
{
    /// <summary>
    /// Start the task or workflow [taskId] with [parameters] (the input of the execution, validated
    /// against the <see cref="BaseAutomationTask.InputSchema"/> of the element).
    /// Returns as soon as the execution is started, without waiting for it to finish : the progress
    /// is then followed through <see cref="IHistoryService.InstanceUpdated"/>.
    /// </summary>
    /// <exception cref="ExecutionException">If the element can't be started (unknown, not a task or workflow, ...).</exception>
    public Task<TaskInstance> StartAsync(Guid taskId, JToken? parameters = null);

    /// <summary>
    /// Resume the suspended run [runId] this application started : the nodes it was running run
    /// again, the rest of it carrying on from what the history holds. Returns as soon as it runs,
    /// like <see cref="StartAsync"/>.
    /// </summary>
    /// <exception cref="ExecutionException">The run can't be resumed, see <see cref="Data.Execution.RunResumption"/>.</exception>
    public Task<TaskInstance> ResumeAsync(Guid runId);

    /// <summary>
    /// Wait for the execution [instanceId] to be finished and return its final instance.
    /// Returns the persisted instance right away when the execution is already over.
    /// </summary>
    public Task<TaskInstance> WaitAsync(Guid instanceId);

    /// <summary>
    /// Cancel the running execution [instanceId], every node it runs included. A paused or
    /// suspended run of this application is canceled rather than left to be resumed. A no-op for
    /// a run that is over.
    /// </summary>
    /// <exception cref="ExecutionException">The run is run by another application.</exception>
    public Task CancelAsync(Guid instanceId);

    /// <summary>
    /// Pause the running workflow [instanceId] : the nodes it runs finish, no other task starts, and
    /// it ends <see cref="EnumTaskState.Suspended"/>, to be resumed (see <see cref="ResumeAsync"/>). A
    /// task on its own simply runs to its end.
    /// </summary>
    /// <exception cref="ExecutionException">The run isn't run by this application.</exception>
    public Task PauseAsync(Guid instanceId);

    /// <summary>
    /// The executions currently running, most recently started first.
    /// </summary>
    public IReadOnlyCollection<TaskInstance> GetRunning();

    /// <summary>
    /// Raised when an execution starts or ends, so <see cref="GetRunning"/> changed.
    /// </summary>
    public event Action? RunningChanged;
}
