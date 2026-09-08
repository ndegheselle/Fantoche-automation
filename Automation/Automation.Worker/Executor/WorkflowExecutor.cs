using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Graph;
using Automation.Worker.Packages;
using Newtonsoft.Json.Linq;

namespace Automation.Worker.Executor;

/// <summary>
/// Where a run of a workflow stands : the walk of the graph (see <see cref="GraphExecutionContext"/>)
/// plus the instance of the workflow it belongs to, which the branches are closed by.
/// </summary>
public record WorkflowExecutionContext : GraphExecutionContext
{
    public required WorkflowInstance WorkflowInstance { get; init; }
}

public class WorkflowExecutor
{
    private readonly NodeExecutor _executor;

    public WorkflowExecutor(IPackageManagement packageManagement)
    {
        _executor = new NodeExecutor(packageManagement, this);
    }

    // FIXME : in the workflowInstance the Workflow should have a Graph up to date, this should be indicated somewhere or forced
    public async Task<WorkflowInstance> ExecuteAsync(
        WorkflowInstance workflowInstance,
        TaskInstancesProgress? progress = null,
        CancellationToken? cancellation = null)
    {
        // Combine external cancellation with workflow's own CTS (used for StopAtFirstEnd)
        using var linkedCts = cancellation.HasValue
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellation.Value, workflowInstance.WorkflowCts.Token)
            : null;
        var token = (CancellationToken?)(linkedCts?.Token ?? workflowInstance.WorkflowCts.Token);

        // XXX : should check if the workflow instance is correctly formated (parameters if there is an InputSchema)

        // The context of the scopes the workflow belongs to is read once, when the run starts : it
        // is what the mappings of its nodes read as "$global.*".
        GraphContextResolution resolution = new GraphContextResolution(workflowInstance.Id)
        {
            GlobalContext = workflowInstance.GlobalContext,
        };
        // Create start tasks instances (there should be only one)
        var startTasks = new List<Task<IReadOnlyList<TaskInstance>>>();
        foreach (var start in workflowInstance.Workflow.Graph.GetStartNodes())
        {
            // Apply node default on the given parameters
            var startParameters = GraphContextResolution.MergeContexts(start.InputTemplate, workflowInstance.Parameters);
            var startInstance = resolution.CreateInstance(start, startParameters, EnumTaskState.Completed);
            startInstance.Output = startParameters;
            progress?.StateChanges?.Report(startInstance);

            WorkflowExecutionContext context = new()
            {
                Resolution = resolution,
                Graph = workflowInstance.Workflow.Graph,
                Node = start,
                Instance = startInstance,
                WorkflowInstance = workflowInstance,
            };
            startTasks.Add(NextAsync(context, progress, token));
        }

        var results = await Task.WhenAll(startTasks);
        var endInstances = results.SelectMany(r => r).ToList();

        return EndAsync(workflowInstance, endInstances, progress);
    }

    private async Task<IReadOnlyList<TaskInstance>> NextAsync(
        WorkflowExecutionContext context,
        TaskInstancesProgress? progress,
        CancellationToken? cancellation)
    {
        var nextPairs = context.Graph.GetNext(context.Node);

        var branches = new List<Task<IReadOnlyList<TaskInstance>>>();
        foreach (var pair in nextPairs)
        {
            var next = pair.Task;
            var nextContext = context with { Node = next };
            branches.Add(RunBranchAsync(nextContext, progress, cancellation));
        }

        var endInstances = new List<TaskInstance>();
        if (branches.Count > 0)
        {
            var results = await Task.WhenAll(branches);
            foreach (var r in results)
                endInstances.AddRange(r);
        }

        return endInstances;
    }

    /// <summary>
    /// Run a branch of the workflow, return the end instances reached by this branch. The end instances can be empty if the task fail, is canceled or doesn't have outputs connections.
    /// </summary>
    /// <param name="node"></param>
    /// <param name="previousInstance"></param>
    /// <param name="workflowInstance"></param>
    /// <param name="progress"></param>
    /// <param name="cancellation"></param>
    /// <returns>All last branchs instances</returns>
    /// <exception cref="Exception"></exception>
    private async Task<IReadOnlyList<TaskInstance>> RunBranchAsync(
        WorkflowExecutionContext context,
        TaskInstancesProgress? progress,
        CancellationToken? cancellation)
    {
        // Control nodes are driven by the workflow itself, they never reach the node executor.
        if (context.Node is GraphControl control)
            return await RunControlBranchAsync(context, progress, cancellation);

        var parameters = context.Resolution.GetInputFor(context.Node, context.Instance, context.Instance.Shared);

        if (parameters.HasError)
            throw new ExecutionException($"Parameters error in node [{context.Node.Id}] : {string.Join('\n', parameters.Errors)}");

        var instance = context.Resolution.CreateInstance(context.Node, parameters.Token, EnumTaskState.Progressing, context.Instance);
        // The shared context walks down the branch, whatever a share upstream fed it with.
        instance.Shared = context.Instance.Shared;
        progress?.StateChanges?.Report(instance);
        instance = await _executor.ExecuteAsync(
            context.Node.AutomationTask ?? throw new ExecutionException("Workflow tasks are not loaded (is the graph refreshed?)."),
            instance,
            progress,
            cancellation);
        progress?.StateChanges?.Report(instance);

        if (instance.State == EnumTaskState.Completed && instance.Output != null)
            return await NextAsync(context with { Instance = instance }, progress, cancellation);

        return [];
    }

    #region Control tasks
    /// <summary>
    /// Run a control node of the branch : a control has no task to execute, it only drives the
    /// workflow (merge the branches, feed the shared context, close the workflow).
    /// </summary>
    private async Task<IReadOnlyList<TaskInstance>> RunControlBranchAsync(
        WorkflowExecutionContext context,
        TaskInstancesProgress? progress,
        CancellationToken? cancellation)
    {
        GraphControl control = context.Node as GraphControl ?? throw new ArgumentException("Node is not a graph control", nameof(context));

        // A join holds the branch until every other one reached it, the rest of the controls are
        // passed through by each branch reaching them.
        TaskInstance? instance = control.IsJoin()
            ? ResolveJoin(context, control, progress)
            : ResolveControl(context, control);

        // The join is still waiting for the branches that have yet to arrive.
        if (instance == null)
            return [];

        // A control produces nothing of its own, it hands over its resolved parameters.
        instance.Output = instance.Parameters ?? new JObject();
        instance.State = EnumTaskState.Completed;
        progress?.StateChanges?.Report(instance);

        // The end closes the branch, its instance is the result of the workflow.
        if (control.IsEnd())
        {
            // Cancel every other task that may be still running
            context.WorkflowInstance.WorkflowCts.Cancel();
            return [instance];
        }

        return await NextAsync(context with { Instance = instance }, progress, cancellation);
    }

    /// <summary>
    /// Instance of a control reached by a single branch (a share, a map or the end) : it resolves
    /// against what that branch carries and runs again each time a branch reaches it.
    /// </summary>
    private TaskInstance ResolveControl(WorkflowExecutionContext context, GraphControl control)
    {
        var input = context.Resolution.GetInputFor(control, context.Instance, context.Instance.Shared);

        if (input.HasError)
            throw new ExecutionException($"Parameters error in control [{control.Id}] : {string.Join('\n', input.Errors)}");

        var instance = context.Resolution.CreateInstance(control, input.Token, EnumTaskState.Progressing, context.Instance);
        // A share feeds what it resolved to everything downstream, the other controls hand the
        // shared context over as they got it.
        instance.Shared = control.IsShare()
            ? GraphContextResolution.MergeContexts(context.Instance.Shared, input.Token)
            : context.Instance.Shared;

        return instance;
    }

    /// <summary>
    /// Instance of a join, null while some of its branches have yet to reach it : the last one
    /// arriving resumes the waiting instance with what every branch produced.
    /// </summary>
    private TaskInstance? ResolveJoin(WorkflowExecutionContext context, GraphControl control, TaskInstancesProgress? progress)
    {
        var previousNodes = context.Graph.GetPrevious(control).ToList();

        if (!context.Resolution.TryJoinBranches(control, context.Instance, previousNodes, out var instance, out var branchesInstances))
            return null;

        // Merge every branche shared data
        JToken? shared = branchesInstances.Aggregate(
            context.Instance.Shared,
            (merged, branch) => GraphContextResolution.MergeContexts(merged, branch.Shared));

        var input = context.Resolution.GetInputFor(control, branchesInstances, shared);
        if (input.HasError)
            throw new ExecutionException($"Parameters error in control [{control.Id}] : {string.Join('\n', input.Errors)}");

        // The branch resuming the join is the one it carries on from.
        instance.Previous = context.Instance;
        instance.Parameters = input.Token;
        instance.Shared = shared;
        instance.State = EnumTaskState.Progressing;

        return instance;
    }
    #endregion

    private WorkflowInstance EndAsync(WorkflowInstance workflowInstance, IReadOnlyList<TaskInstance> endInstances, TaskInstancesProgress? progress = null)
    {
        if (workflowInstance.Workflow.OutputSchema != null && endInstances.Count == 0)
            throw new ExecutionException("Reached end of workflow without data.");

        if (endInstances.Count > 1)
            workflowInstance.WorkflowCts.Cancel();

        workflowInstance.Output = endInstances.FirstOrDefault()?.Output;

        workflowInstance.State = EnumTaskState.Completed;
        progress?.StateChanges?.Report(workflowInstance);
        return workflowInstance;
    }
}