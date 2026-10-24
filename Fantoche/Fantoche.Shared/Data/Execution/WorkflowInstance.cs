using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using Fantoche.Plugins.Shared;
using Fantoche.Shared.Data.Scoped;
using Newtonsoft.Json.Linq;

namespace Fantoche.Shared.Data.Execution;

public struct TaskInstancesProgress
{
    public IProgress<TaskNotification>? Notifications { get; set; }
    public IProgress<TaskInstance>? StateChanges { get; set; }
}

/// <summary>
/// Instance of a workflow execution. Carries both the persisted task-instance data
/// (id, state, input/output, ...) and the runtime data needed to drive the execution
/// (graph definition, shared token, child node instances, cancellation).
/// </summary>
public class WorkflowInstance : TaskInstance
{
    /// <summary>
    /// Workflow definition being executed.
    /// </summary>
    [JsonIgnore]
    public AutomationWorkflow Workflow { get; }

    /// <summary>
    /// What the history holds of a run being resumed : every instance it ran, the ones of its
    /// nested runs included. Empty for a run starting.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyList<TaskInstance> StoredInstances { get; set; } = [];

    /// <summary>
    /// Requested once the run is to pause : the nodes running finish, no other task starts, and
    /// the run ends <see cref="EnumTaskState.Suspended"/>. Handed to its nested runs.
    /// </summary>
    [JsonIgnore]
    public CancellationToken PauseRequested { get; set; }

    /// <summary>
    /// Instances created during this workflow execution, indexed by graph node id.
    /// </summary>
    [JsonIgnore]
    public ConcurrentDictionary<Guid, List<TaskInstance>> NodeInstances { get; } = [];

    /// <summary>
    /// Cancellation source owned by the workflow
    /// </summary>
    [JsonIgnore]
    public CancellationTokenSource WorkflowCts { get; } = new();

    public WorkflowInstance(AutomationWorkflow workflow)
    {
        Workflow = workflow;
        TaskId = workflow.Id;
        WorkflowVersion = workflow.Version;
    }

    /// <summary>
    /// [stored], an instance of [workflow] as the history or a message holds it, made one that can
    /// drive the workflow again.
    /// </summary>
    public static WorkflowInstance From(AutomationWorkflow workflow, TaskInstance stored)
    {
        WorkflowInstance instance = new(workflow)
        {
            Id = stored.Id,
            NodeId = stored.NodeId,
            ParentInstanceId = stored.ParentInstanceId,
            NodeName = stored.NodeName,
            Shared = stored.Shared,
            Parameters = stored.Parameters,
            Output = stored.Output,
            GlobalContext = stored.GlobalContext,
            ApplicationId = stored.ApplicationId,
            SessionId = stored.SessionId,
            NestedVersions = stored.NestedVersions,
            WorkflowVersion = stored.WorkflowVersion,
            PreviousIds = stored.PreviousIds,
            CreatedAt = stored.CreatedAt,
            State = stored.State,
        };
        // Assigning the state stamps FinishedAt, the stored value has to stay the one of the run.
        instance.FinishedAt = stored.FinishedAt;
        return instance;
    }
}