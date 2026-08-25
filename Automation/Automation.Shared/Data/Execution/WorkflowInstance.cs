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
    [Newtonsoft.Json.JsonIgnore]
    public AutomationWorkflow Workflow { get; }

    [Newtonsoft.Json.JsonIgnore]
    public JToken? GlobalContext { get; }
    [Newtonsoft.Json.JsonIgnore]
    public JToken? SharedContext { get; set; }

    /// <summary>
    /// Instances created during this workflow execution, indexed by graph node id.
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    public ConcurrentDictionary<Guid, List<TaskInstance>> NodeInstances { get; } = [];

    /// <summary>
    /// Cancellation source owned by the workflow (used by StopAtFirstEnd).
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    public CancellationTokenSource WorkflowCts { get; } = new();

    private readonly object _lock = new();

    [JsonIgnore]
    public GraphExecutionContext Execution { get; private set; }

    public WorkflowInstance(AutomationWorkflow workflow)
    {
        Workflow = workflow;
        TaskId = workflow.Id;
        Execution = new GraphExecutionContext(Workflow.Graph, this);
    }

    public TaskInstance CreateInstance(BaseGraphTask node, JToken? parameters, EnumTaskState state = EnumTaskState.Pending, TaskInstance? previous = null)
    {
        TaskInstance instance;
        
        if (node.AutomationTask is AutomationWorkflow workflow)
            instance = new WorkflowInstance(workflow);
        else
            instance = new TaskInstance();

        instance.ParentInstanceId = Id;
        instance.ParentWorkflow = this;
        instance.TaskId = node.TaskId;
        instance.NodeId = node.Id;
        instance.NodeName = node.Name;
        instance.Node = node;
        instance.Parameters = parameters;
        instance.State = state;

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
            // CreateInstance also takes _lock - reentrant, no deadlock
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
