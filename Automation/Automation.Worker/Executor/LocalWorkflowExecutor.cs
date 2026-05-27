using Automation.Plugins.Shared;
using Automation.Shared.Data;
using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Graph;
using Automation.Shared.Data.Scoped;
using Automation.Worker.Packages;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;

namespace Automation.Worker.Executor;

public struct WorkflowChanges
{
    public delegate void InstanceChangeDelegate(TaskInstance instance);
    public InstanceChangeDelegate OnInstanceChange { get; set; }
}

public class LocalWorkflowContext
{
    public AutomationWorkflow Workflow { get; }

    public TaskInstance WorkflowInstance { get; }

    /// <summary>
    /// Shared context between tasks, initialized with the workflow parent context.
    /// </summary>
    public JToken? SharedToken { get; }

    /// <summary>
    /// Instances created during this workflow execution, indexed by graph node id.
    /// </summary>
    public ConcurrentDictionary<Guid, List<TaskInstance>> NodeInstances { get; } = [];

    public CancellationTokenSource WorkflowCts { get; } = new();

    private readonly object _lock = new();
    private readonly WorkflowChanges? _changes;

    public LocalWorkflowContext(AutomationWorkflow workflow, Guid? parentWorkflowInstanceId, JToken? sharedToken = null, WorkflowChanges? changes = null)
    {
        _changes = changes;
        Workflow = workflow;
        SharedToken = sharedToken;
        WorkflowInstance = new TaskInstance()
        {
            ParentInstanceId = parentWorkflowInstanceId,
            TaskId = workflow.Id,
            State = EnumTaskState.Progressing,
        };

        _changes?.OnInstanceChange(WorkflowInstance);
    }

    public TaskInstance CreateInstance(BaseGraphTask node, JToken? input, EnumTaskState state = EnumTaskState.Pending, TaskInstance? previous = null)
    {
        var instance = new TaskInstance
        {
            ParentInstanceId = WorkflowInstance.Id,
            TaskId = node.TaskId,
            NodeId = node.Id,
            NodeName = node.Name,
            Input = input,
            State = state,
            FinishedAt = (state & EnumTaskState.Finished) != 0 ? DateTime.UtcNow : null,
        };

        if (previous != null)
            instance.Link(previous);

        lock (_lock)
        {
            if (!NodeInstances.TryGetValue(node.Id, out var list))
                NodeInstances[node.Id] = list = [];
            list.Add(instance);
        }

        _changes?.OnInstanceChange(instance);

        return instance;
    }

    public TaskInstance UpdateInstance(TaskInstance instance, JToken? input, JToken? output, EnumTaskState state)
    {
        instance.Input = input;
        instance.Output = output;
        instance.State = state;

        _changes?.OnInstanceChange(instance);
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
    /// Ensures a Waiting instance exists for this wait-all node, and atomically claims it
    /// (transitions to Progressing) when all predecessors have run. Returns the claimed instance
    /// to exactly one caller; all others get null.
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
                // If one of the previous doesn't have a completed node instance we stop there
                if (previousInstance == null)
                    return null;
                previousInstances.Add(previousInstance);
            }

            return previousInstances;
        }
    }
}

public class LocalWorkflowExecutor
{
    private readonly LocalNodeExecutor _executor;
    private readonly WorkflowChanges? _changes;

    public LocalWorkflowExecutor(IPackageManagement packageManagement, WorkflowChanges? changes)
    {
        _executor = new LocalNodeExecutor(packageManagement, this);
        _changes = changes;
    }

    public async Task<TaskOutput> ExecuteAsync(
        AutomationWorkflow workflow,
        JToken? input,
        JToken? sharedToken = null,
        IProgress<TaskNotification>? notifications = null,
        CancellationToken? cancellation = null)
    {
        if (workflow.Graph.IsRefreshed == false)
            throw new Exception("The workflow graph should be refreshed before being executed.");

        var context = new LocalWorkflowContext(workflow, sharedToken, changes: _changes);

        // Combine external cancellation with workflow's own CTS (used for StopAtFirstEnd)
        using var linkedCts = cancellation.HasValue
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellation.Value, context.WorkflowCts.Token)
            : null;
        var token = (CancellationToken?)(linkedCts?.Token ?? context.WorkflowCts.Token);

        var startTasks = new List<Task<IReadOnlyList<TaskInstance>>>();
        foreach (var start in workflow.Graph.GetStartNodes())
        {
            startTasks.Add(NextAsync(start, startInstance, context, null, token));
        }

        var results = await Task.WhenAll(startTasks);
        var endInstances = results.SelectMany(r => r).ToList();
        return EndAsync(context, endInstances);
    }

    private async Task<IReadOnlyList<TaskInstance>> NextAsync(
        BaseGraphTask current,
        TaskInstance currentInstance,
        LocalWorkflowContext context,
        HashSet<Guid>? activeOutputConnectorIds,
        CancellationToken? cancellation)
    {
        var nextPairs = context.Workflow.Graph.GetNext(current);

        // When a control task activates only specific outputs, skip the rest
        if (activeOutputConnectorIds != null)
            nextPairs = nextPairs.Where(x => activeOutputConnectorIds.Contains(x.SourceConnector.Id));

        var endInstances = new List<TaskInstance>();
        var branches = new List<Task<IReadOnlyList<TaskInstance>>>();
        foreach (var pair in nextPairs)
        {
            var next = pair.Task;

            if (next.TaskId == AutomationControl.EndTask.Id)
            {
                endInstances.Add(currentInstance);
                if (context.Workflow.WorkflowSettings.StopAtFirstEnd)
                    context.WorkflowCts.Cancel();
                continue;
            }

            branches.Add(RunBranchAsync(next, currentInstance, context, cancellation));
        }

        if (branches.Count > 0)
        {
            var results = await Task.WhenAll(branches);
            foreach (var r in results)
                endInstances.AddRange(r);
        }

        return endInstances;
    }

    private async Task<IReadOnlyList<TaskInstance>> RunBranchAsync(
        BaseGraphTask node,
        TaskInstance previousInstance,
        LocalWorkflowContext context,
        CancellationToken? cancellation)
    {
        TaskInstance? existingInstance = null;
        IReadOnlyList<TaskInstance>? previousInstances = null;
        // If the task wait for all inputs but only have one we treat it like other tasks and skip this part
        if (node.Settings.IsWaitingAllInputs && context.Workflow.Graph.WithMultipleInputsConnections(node))
        {
            existingInstance = context.GetOrCreateWaitingInstance(node, previousInstance);
            previousInstances = context.TryGetAllPrevious(node);

            // All previous are not ready yet
            if (previousInstances == null || previousInstances.Count == 0)
                return [];

            foreach (var prev in previousInstances)
                existingInstance.Link(prev);
        }

        previousInstances ??= [previousInstance];
        JToken? input = null;
        if (!string.IsNullOrEmpty(node.InputJson))
        {
            var taskContext = context.Workflow.Graph.Execution.GetContextFor(node, previousInstances, context.SharedToken);
            input = ReferencesHandler.ReplaceReferences(JToken.Parse(node.InputJson), taskContext).ReplacedSetting;
        }

        // Set the instance as progressing
        if (existingInstance == null)
            existingInstance = context.CreateInstance(node, input, EnumTaskState.Progressing, previousInstance);
        else
            existingInstance = context.UpdateInstance(existingInstance, input, null, EnumTaskState.Progressing);

        try
        {
            var output = await _executor.ExecuteAsync(
                node.AutomationTask ?? throw new Exception("Workflow tasks are not loaded (is the graph refreshed?)."),
                new LocalExecutionContext { Shared = context.SharedToken, Input = input, CurrentNode = node },
                null,
                cancellation);

            // Set the end state of the instance
            existingInstance = context.UpdateInstance(existingInstance, input, output.OutputToken, output.State);

            if (output.State == EnumTaskState.Completed)
                return await NextAsync(node, existingInstance, context, output.ActiveOutputConnectorIds, cancellation);

            return [];
        }
        catch (OperationCanceledException)
        {
            context.UpdateInstance(existingInstance, existingInstance.Input, null, EnumTaskState.Canceled);
            return [];
        }
    }

    private TaskOutput EndAsync(LocalWorkflowContext context, IReadOnlyList<TaskInstance> endInstances)
    {
        // TODO : return failed and handle task instance on the level of the workflow
        if (context.Workflow.OutputSchema != null && endInstances.Count == 0)
            throw new Exception("Reached end of workflow without data.");

        return new TaskOutput
        {
            OutputToken = context.Workflow.Graph.Execution.CombineEndOutputs(endInstances, context.Workflow.WorkflowSettings),
            State = EnumTaskState.Completed
        };
    }
}
