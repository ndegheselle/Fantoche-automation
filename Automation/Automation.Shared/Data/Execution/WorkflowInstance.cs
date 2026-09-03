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
    public JToken? GlobalContext { get; set; }
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

    /// <summary>
    /// Last instance of [node] in [state], the most recently finished one first.
    /// </summary>
    public TaskInstance? GetLastNodeInstance(BaseGraphTask node, EnumTaskState state)
    {
        lock (_lock)
        {
            NodeInstances.TryGetValue(node.Id, out var list);
            return list?.OrderByDescending(x => x.FinishedAt).FirstOrDefault(i => i.State == state);
        }
    }

    /// <summary>
    /// Last instance of [node] having handed an output over, so the one the nodes after it read.
    /// Null when the node hasn't run yet or when every run of it died (see
    /// <see cref="BranchLiveness.HasDelivered"/>).
    /// </summary>
    public TaskInstance? GetLastDeliveredInstance(BaseGraphTask node)
    {
        lock (_lock)
        {
            NodeInstances.TryGetValue(node.Id, out var list);
            return list?.Where(BranchLiveness.HasDelivered)
                        .OrderByDescending(x => x.FinishedAt)
                        .FirstOrDefault();
        }
    }

    /// <summary>
    /// Instances of [node] created during this run.
    /// </summary>
    public IReadOnlyList<TaskInstance> GetNodeInstances(BaseGraphTask node)
    {
        lock (_lock)
        {
            if (NodeInstances.TryGetValue(node.Id, out var list))
                return list.ToArray();
            return [];
        }
    }

    public TaskInstance GetOrCreateWaitingInstance(BaseGraphTask node, TaskInstance previousInstance)
    {
        lock (_lock)
        {
            var existing = GetLastNodeInstance(node, EnumTaskState.Waiting);
            if (existing != null)
                return existing;
            // CreateInstance also takes _lock - reentrant, no deadlock
            return CreateInstance(node, null, EnumTaskState.Waiting, previousInstance);
        }
    }

    /// <summary>
    /// Instances currently holding on the branches reaching their node.
    /// </summary>
    public IReadOnlyList<TaskInstance> GetWaitingInstances()
    {
        lock (_lock)
        {
            return NodeInstances.Values
                .SelectMany(x => x)
                .Where(x => x.State == EnumTaskState.Waiting)
                .ToArray();
        }
    }

    /// <summary>
    /// Take hold of a waiting instance and move it to [state]. Only the caller getting
    /// <c>true</c> runs the node : the branches racing to resume it (the last one arriving, one
    /// of them dying) get <c>false</c>.
    /// </summary>
    public bool TryClaimWaitingInstance(TaskInstance instance, EnumTaskState state)
    {
        lock (_lock)
        {
            if (instance.State != EnumTaskState.Waiting)
                return false;
            instance.State = state;
            return true;
        }
    }

    /// <summary>
    /// Gather the instances a node waiting on all of its branches reads, telling whether it can
    /// run at all :
    /// <list type="bullet">
    /// <item><see cref="EnumWaitResolution.Pending"/> : a branch can still deliver, keep waiting.</item>
    /// <item><see cref="EnumWaitResolution.Resolved"/> : [previous] holds what the live branches gave.</item>
    /// <item><see cref="EnumWaitResolution.Dead"/> : every branch is dead, the node will never run.</item>
    /// </list>
    /// <para>
    /// A dead branch is left out instead of being waited for : nothing can come out of it anymore,
    /// so the node runs with the branches that did deliver. References of the node pointing into a
    /// dead branch are simply not resolved, that branch never having produced anything.
    /// </para>
    /// </summary>
    public EnumWaitResolution TryResolvePrevious(BaseGraphTask node, out IReadOnlyList<TaskInstance> previous)
    {
        List<TaskInstance> delivered = [];
        bool pending = false;

        lock (_lock)
        {
            foreach (BaseGraphTask p in Workflow.Graph.GetPrevious(node).DistinctBy(x => x.Id))
            {
                TaskInstance? instance = GetLastDeliveredInstance(p);
                if (instance != null)
                {
                    delivered.Add(instance);
                    continue;
                }

                if (BranchLiveness.Resolve(Workflow.Graph, GetNodeInstances, p) != EnumBranchLiveness.Dead)
                    pending = true;
            }
        }

        previous = delivered;
        if (pending)
            return EnumWaitResolution.Pending;
        return delivered.Count > 0 ? EnumWaitResolution.Resolved : EnumWaitResolution.Dead;
    }
}
