using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Scoped;
using Newtonsoft.Json.Linq;

namespace Automation.Shared.Services;

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
    /// Wait for the execution [instanceId] to be finished and return its final instance.
    /// Returns the persisted instance right away when the execution is already over.
    /// </summary>
    public Task<TaskInstance> WaitAsync(Guid instanceId);

    /// <summary>
    /// Cancel the running execution [instanceId], a no-op if it isn't running anymore.
    /// </summary>
    public Task CancelAsync(Guid instanceId);

    /// <summary>
    /// The executions currently running, most recently started first.
    /// </summary>
    public IReadOnlyCollection<TaskInstance> GetRunning();
}
