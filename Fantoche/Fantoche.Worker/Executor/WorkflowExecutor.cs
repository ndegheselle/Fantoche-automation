using Fantoche.Plugins.Shared;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Graph;
using Fantoche.Worker.Packages;
using Newtonsoft.Json.Linq;

namespace Fantoche.Worker.Executor;

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
    private readonly ITaskExecutor _executor;

    /// <summary>A walk running every node in this process.</summary>
    public WorkflowExecutor(TaskLoaders tasks, IStorage storage)
    {
        _executor = new LocalTaskExecutor(tasks, storage, this);
    }

    /// <summary>A walk running every node with [executor].</summary>
    public WorkflowExecutor(ITaskExecutor executor)
    {
        _executor = executor;
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
            PauseRequested = workflowInstance.PauseRequested,
        };

        // A resumed run carries on from what its history holds, a new one from the start of its graph.
        List<TaskInstance> stored = [.. workflowInstance.StoredInstances.Where(x => x.ParentInstanceId == workflowInstance.Id)];
        List<Task<IReadOnlyList<TaskInstance>>> branches = stored.Count == 0
            ? Start(workflowInstance, resolution, progress, token)
            : Resume(workflowInstance, resolution, stored, progress, token);

        var results = await Task.WhenAll(branches);
        var endInstances = results.SelectMany(r => r).ToList();

        // Paused before reaching its end : what it didn't start is what a resume carries on with.
        if (endInstances.Count == 0 && workflowInstance.PauseRequested.IsCancellationRequested)
        {
            workflowInstance.State = EnumTaskState.Suspended;
            workflowInstance.Output = JToken.FromObject("Paused by the user.");
            progress?.StateChanges?.Report(workflowInstance);
            return workflowInstance;
        }

        // Held by a node waiting for the user : the answer resumes the run from there.
        bool isAwaitingUser = resolution.NodeInstances.Values.SelectMany(x => x).Any(x => x.State == EnumTaskState.AwaitingUser);
        if (endInstances.Count == 0 && isAwaitingUser)
        {
            workflowInstance.State = EnumTaskState.AwaitingUser;
            progress?.StateChanges?.Report(workflowInstance);
            return workflowInstance;
        }

        return EndAsync(workflowInstance, endInstances, progress);
    }

    /// <summary>The branches leaving the start of the graph (there should be only one).</summary>
    private List<Task<IReadOnlyList<TaskInstance>>> Start(
        WorkflowInstance workflowInstance,
        GraphContextResolution resolution,
        TaskInstancesProgress? progress,
        CancellationToken? cancellation)
    {
        List<Task<IReadOnlyList<TaskInstance>>> branches = [];
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
            branches.Add(NextAsync(context, progress, cancellation));
        }
        return branches;
    }

    /// <summary>
    /// The branches carrying a resumed run on from [stored], what its history holds of its own
    /// instances : the nodes it was running run again from the start, and each completed node
    /// leads to the nodes after it that it hadn't reached yet. A join still waiting for its
    /// branches waits on, the ones that already arrived being counted.
    /// </summary>
    private List<Task<IReadOnlyList<TaskInstance>>> Resume(
        WorkflowInstance workflowInstance,
        GraphContextResolution resolution,
        List<TaskInstance> stored,
        TaskInstancesProgress? progress,
        CancellationToken? cancellation)
    {
        IReadOnlyList<TaskInstance> restored = resolution.Restore(workflowInstance.Workflow.Graph, stored);

        // The run reached its end : only its own instance was left to close.
        TaskInstance? end = restored.FirstOrDefault(x => x.State == EnumTaskState.Completed && x.Node is GraphControl control && control.IsEnd());
        if (end != null)
            return [Task.FromResult<IReadOnlyList<TaskInstance>>([end])];

        // The nodes each instance led to, a join holding every branch that reached it.
        HashSet<(Guid PreviousId, Guid NodeId)> reached = [.. restored.SelectMany(x => x.PreviousIds.Select(id => (id, x.Node!.Id)))];

        List<Task<IReadOnlyList<TaskInstance>>> branches = [];
        foreach (TaskInstance instance in restored)
        {
            // A nested run being run again carries on from its own history.
            if (instance is WorkflowInstance nested)
                nested.StoredInstances = workflowInstance.StoredInstances;

            WorkflowExecutionContext context = new()
            {
                Resolution = resolution,
                Graph = workflowInstance.Workflow.Graph,
                Node = instance.Node!,
                Instance = instance,
                WorkflowInstance = workflowInstance,
            };

            // Only a task can be left running : a control is reported once resolved, a join waiting
            // being the exception, and it waits on.
            // A nested run paused along with this one, or held by its own user interface, resumes
            // the same way. A user interface task itself waits on until it is answered.
            bool isHeldNestedRun = instance is WorkflowInstance && instance.State == EnumTaskState.AwaitingUser;
            bool wasRunning = isHeldNestedRun
                || instance.State is EnumTaskState.Pending or EnumTaskState.Progressing or EnumTaskState.Suspended;
            if (wasRunning && instance.Node is not GraphControl)
            {
                instance.State = EnumTaskState.Progressing;
                branches.Add(RunTaskAsync(context, instance, progress, cancellation));
                continue;
            }

            if (instance.State != EnumTaskState.Completed || instance.Output == null)
                continue;

            IEnumerable<BaseGraphTask> notReached = context.Graph.GetNext(instance.Node!)
                .Select(x => x.Task)
                .Where(x => !reached.Contains((instance.Id, x.Id)));
            foreach (BaseGraphTask next in notReached)
                branches.Add(RunBranchAsync(context with { Node = next }, progress, cancellation));
        }
        return branches;
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

        // Pausing : the task isn't started, a resume finds the node before it with nothing after.
        // The controls still pass, running no code of the user : a run whose last task is over
        // reaches its end rather than pausing on it.
        if (context.WorkflowInstance.PauseRequested.IsCancellationRequested)
            return [];

        var parameters = context.Resolution.GetInputFor(context.Node, context.Instance, context.Instance.Shared);

        if (parameters.HasError)
            throw new ExecutionException($"Parameters error in node [{context.Node.Id}] : {string.Join('\n', parameters.Errors)}");

        var instance = context.Resolution.CreateInstance(context.Node, parameters.Token, EnumTaskState.Progressing, context.Instance);
        // The shared context walks down the branch, whatever a share upstream fed it with.
        instance.Shared = context.Instance.Shared;
        return await RunTaskAsync(context, instance, progress, cancellation);
    }

    /// <summary>
    /// Run [instance] of the task [context] reached, then the nodes after it when it completed with
    /// something to hand over.
    /// </summary>
    private async Task<IReadOnlyList<TaskInstance>> RunTaskAsync(
        WorkflowExecutionContext context,
        TaskInstance instance,
        TaskInstancesProgress? progress,
        CancellationToken? cancellation)
    {
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

        // Reported while it waits so a resumed run knows which branches already arrived.
        bool isResumed = context.Resolution.TryJoinBranches(
            control,
            context.Instance,
            previousNodes,
            out var instance,
            out var branchesInstances,
            waiting => progress?.StateChanges?.Report(waiting));
        if (!isResumed)
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