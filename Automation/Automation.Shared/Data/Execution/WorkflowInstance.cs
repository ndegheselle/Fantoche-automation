using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using Automation.Plugins.Shared;
using Automation.Shared.Data.Graph;
using Automation.Shared.Data.Scoped;
using Newtonsoft.Json.Linq;

namespace Automation.Shared.Data.Execution;

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
    [Newtonsoft.Json.JsonIgnore]
    public AutomationWorkflow Workflow { get; }

    /// <summary>
    /// Shared context inherited from a parent workflow, propagated to every task of this workflow.
    /// </summary>
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public JToken? SharedToken { get; }

    /// <summary>
    /// Instances created during this workflow execution, indexed by graph node id.
    /// </summary>
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public ConcurrentDictionary<Guid, List<TaskInstance>> NodeInstances { get; } = [];

    /// <summary>
    /// Cancellation source owned by the workflow (used by StopAtFirstEnd).
    /// </summary>
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public CancellationTokenSource WorkflowCts { get; } = new();

    private readonly object _lock = new();

    public WorkflowInstance(
        AutomationWorkflow workflow,
        Guid? parentInstanceId = null,
        JToken? sharedToken = null)
    {
        Workflow = workflow;
        SharedToken = sharedToken;
        ParentInstanceId = parentInstanceId;
        TaskId = workflow.Id;
        NodeName = workflow.Metadata.Name;
        State = EnumTaskState.Progressing;
    }

    public TaskInstance CreateInstance(BaseGraphTask node, JToken? input, EnumTaskState state = EnumTaskState.Pending, TaskInstance? previous = null)
    {
        var instance = new TaskInstance
        {
            ParentInstanceId = Id,
            ParentWorkflow = this,
            TaskId = node.TaskId,
            NodeId = node.Id,
            NodeName = node.Name,
            Node = node,
            Input = input,
            State = state
        };

        if (previous != null)
            instance.Link(previous);

        lock (_lock)
        {
            if (!NodeInstances.TryGetValue(node.Id, out var list))
                NodeInstances[node.Id] = list = [];
            list.Add(instance);
        }

        return instance;
    }

    public TaskInstance? GetLastNodeInstance(BaseGraphTask node, EnumTaskState state)
    {
        lock (_lock)
        {
            NodeInstances.TryGetValue(node.Id, out var list);
            return list?.OrderByDescending(x => x.FinishedAt).FirstOrDefault(i => i.State == state);
        }
    }

    public TaskInstance GetOrCreateWaitingInstance(BaseGraphTask node, TaskInstance previousInstance)
    {
        lock (_lock)
        {
            NodeInstances.TryGetValue(node.Id, out var list);
            var existing = list?.OrderByDescending(x => x.FinishedAt)
                                 .FirstOrDefault(i => i.State == EnumTaskState.Waiting);
            if (existing != null)
                return existing;
            // CreateInstance also takes _lock — reentrant, no deadlock
            return CreateInstance(node, null, EnumTaskState.Waiting, previousInstance);
        }
    }

    /// <summary>
    /// Returns the completed predecessor instances of <paramref name="node"/> if and only if
    /// every predecessor has one; otherwise <c>null</c>.
    /// </summary>
    public IReadOnlyList<TaskInstance>? TryGetAllPrevious(BaseGraphTask node)
    {
        var previous = Workflow.Graph.GetPrevious(node);
        lock (_lock)
        {
            List<TaskInstance> previousInstances = [];
            foreach (var p in previous)
            {
                var previousInstance = GetLastNodeInstance(p, EnumTaskState.Completed);
                if (previousInstance == null)
                    return null;
                previousInstances.Add(previousInstance);
            }

            return previousInstances;
        }
    }
}
