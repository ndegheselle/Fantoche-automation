using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Graph;
using Automation.Worker.Packages;
using Newtonsoft.Json.Linq;

namespace Automation.Worker.Executor;

public record WorkflowExecutionContext
{
    public GraphContextResolution Resolution { get; }
    public BaseGraphTask Node { get; }
    public TaskInstance Instance { get; }
    public WorkflowInstance WorkflowInstance { get; }

    public WorkflowExecutionContext(GraphContextResolution resolution, WorkflowInstance workflowInstance, BaseGraphTask node, TaskInstance instance)
    {
        Resolution = resolution;
        Node = node;
        Instance = instance;
        WorkflowInstance = workflowInstance;
    }

    public WorkflowExecutionContext Copy(BaseGraphTask node, TaskInstance instance) => new WorkflowExecutionContext(Resolution, WorkflowInstance, node, instance);
}

public class WorkflowExecutor
{
    private readonly NodeExecutor _executor;

    public WorkflowExecutor(LocalPackageManagement packageManagement)
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

        // XXX : should check if the workflow instance is correctly formated (parameters there)

        GraphContextResolution resolution = new GraphContextResolution(workflowInstance.Id);
        // Create start tasks instances (there should be only one)
        var startTasks = new List<Task<IReadOnlyList<TaskInstance>>>();
        foreach (var start in workflowInstance.Workflow.Graph.GetStartNodes())
        {
            // Apply node default on the given parameters
            var startParameters = GraphContextResolution.MergeContexts(start.InputTemplate, workflowInstance.Parameters);
            var startInstance = resolution.CreateInstance(start, startParameters, EnumTaskState.Completed);
            startInstance.Output = startParameters;
            progress?.StateChanges?.Report(startInstance);

            WorkflowExecutionContext context = new WorkflowExecutionContext(resolution, workflowInstance, start, startInstance);
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
        var nextPairs = context.WorkflowInstance.Workflow.Graph.GetNext(context.Node);

        var branches = new List<Task<IReadOnlyList<TaskInstance>>>();
        foreach (var pair in nextPairs)
        {
            var next = pair.Task;
            var nextContext = context.Copy(next, context.Instance);
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
        progress?.StateChanges?.Report(instance);
        instance = await _executor.ExecuteAsync(
            context.Node.AutomationTask ?? throw new ExecutionException("Workflow tasks are not loaded (is the graph refreshed?)."),
            instance,
            progress,
            cancellation);
        progress?.StateChanges?.Report(instance);

        if (instance.State == EnumTaskState.Completed && instance.Output != null)
            return await NextAsync(context, progress, cancellation);

        return [];
    }

    /// <summary>
    /// Run a control node of the branch : a control has no task to execute, it only drives the
    /// workflow (merge the branches, feed the shared context, close the workflow).
    /// </summary>
    private async Task<IReadOnlyList<TaskInstance>> RunControlBranchAsync(
        WorkflowExecutionContext context,
        TaskInstancesProgress? progress,
        CancellationToken? cancellation)
    {
        GraphControl control = context.Node as GraphControl ?? throw new ArgumentException("Node is not a graph control", nameof(context.Node));
        TaskInstance instance;
        if (control.IsJoin())
        {
            instance = context.WorkflowInstance.GetOrCreateWaitingInstance(node, previousInstance);
            var previouses = context.WorkflowInstance.TryGetAllPrevious(node);

            // Some branches have yet to reach this node, the last one arriving resumes it.
            if (previouses == null)
            {
                progress?.StateChanges?.Report(instance);
                return [];
            }

            instance.Parameters = context.Node.ResolveInputMapping(workflowInstance.Execution.GetWaitedInstanceContextFor(previouses));
        }
        else
        {
            instance = workflowInstance.CreateInstance(
                node,
                node.ResolveInputMapping(workflowInstance.Execution.GetInstanceContextFor(previousInstance)),
                EnumTaskState.Progressing,
                previousInstance);
        }

        if (control.IsShare())
            workflowInstance.SharedContext = GraphContextResolution.Merge(workflowInstance.SharedContext, instance.Parameters);

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
        
        return await NextAsync(context.Copy(context.Node, instance), progress, cancellation);
    }

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