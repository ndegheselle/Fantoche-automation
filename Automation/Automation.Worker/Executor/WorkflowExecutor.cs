using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Graph;
using Automation.Worker.Packages;
using Newtonsoft.Json.Linq;

namespace Automation.Worker.Executor;

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

        // Create start tasks instances (there should be only one)
        var startTasks = new List<Task<IReadOnlyList<TaskInstance>>>();
        foreach (var start in workflowInstance.Workflow.Graph.GetStartNodes())
        {
            // The parameters already hold the default values of the start, applied when the input
            // of the workflow was checked : the start only hands them over.
            var startInstance = workflowInstance.CreateInstance(start, workflowInstance.Parameters, EnumTaskState.Completed);
            startInstance.Output = workflowInstance.Parameters;

            progress?.StateChanges?.Report(startInstance);
            startTasks.Add(NextAsync(start, startInstance, workflowInstance, progress, token));
        }

        var results = await Task.WhenAll(startTasks);
        var endInstances = results.SelectMany(r => r).ToList();

        return EndAsync(workflowInstance, endInstances, progress);
    }

    private async Task<IReadOnlyList<TaskInstance>> NextAsync(
        BaseGraphTask current,
        TaskInstance currentInstance,
        WorkflowInstance workflowInstance,
        TaskInstancesProgress? progress,
        CancellationToken? cancellation)
    {
        var nextPairs = workflowInstance.Workflow.Graph.GetNext(current);

        var endInstances = new List<TaskInstance>();
        var branches = new List<Task<IReadOnlyList<TaskInstance>>>();
        foreach (var pair in nextPairs)
        {
            var next = pair.Task;
            branches.Add(RunBranchAsync(next, currentInstance, workflowInstance, progress, cancellation));
        }

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
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private async Task<IReadOnlyList<TaskInstance>> RunBranchAsync(
        BaseGraphTask node,
        TaskInstance previousInstance,
        WorkflowInstance workflowInstance,
        TaskInstancesProgress? progress,
        CancellationToken? cancellation)
    {
        if (!node.IsInputMappingValid)
            throw new NodeExecutionException($"The mapping of '{node.Name}' is not valid JSON.");

        // Control nodes are driven by the workflow itself, they never reach the node executor.
        if (node is GraphControl control)
            return await RunControlBranchAsync(control, previousInstance, workflowInstance, progress, cancellation);

        var parameters = node.ResolveInputMapping(workflowInstance.Execution.GetInstanceContextFor(previousInstance));

        var instance = workflowInstance.CreateInstance(node, parameters, EnumTaskState.Progressing, previousInstance);
        progress?.StateChanges?.Report(instance);
        instance = await _executor.ExecuteAsync(
            node.AutomationTask ?? throw new Exception("Workflow tasks are not loaded (is the graph refreshed?)."),
            instance,
            progress,
            cancellation);
        progress?.StateChanges?.Report(instance);

        if (instance.State == EnumTaskState.Completed && instance.Output != null)
            return await NextAsync(node, instance, workflowInstance, progress, cancellation);

        return [];
    }

    private WorkflowInstance EndAsync(WorkflowInstance workflowInstance, IReadOnlyList<TaskInstance> endInstances, TaskInstancesProgress? progress = null)
    {
        if (workflowInstance.Workflow.OutputSchema != null && endInstances.Count == 0)
            throw new Exception("Reached end of workflow without data.");

        if (workflowInstance.Workflow.WorkflowSettings.StopAtFirstEnd && endInstances.Count > 1)
            throw new NodeExecutionException("Unexcepcted behavior, more than one end instance with StopAtFirst.");

        workflowInstance.Output = endInstances.FirstOrDefault()?.Output;

        workflowInstance.State = EnumTaskState.Completed;
        progress?.StateChanges?.Report(workflowInstance);
        return workflowInstance;
    }

    #region Control tasks
    /// <summary>
    /// Run a control node of the branch : a control has no task to execute, it only drives the
    /// workflow (merge the branches, feed the shared context, close the workflow).
    /// </summary>
    private async Task<IReadOnlyList<TaskInstance>> RunControlBranchAsync(
        GraphControl node,
        TaskInstance previousInstance,
        WorkflowInstance workflowInstance,
        TaskInstancesProgress? progress,
        CancellationToken? cancellation)
    {
        // A join always merges every branch reaching it. An end does the same, unless the workflow
        // stops at the first branch reaching it.
        bool waitAllPrevious = node.IsWaiting(workflowInstance.Workflow.WorkflowSettings.StopAtFirstEnd);

        TaskInstance instance;
        if (waitAllPrevious)
        {
            instance = workflowInstance.GetOrCreateWaitingInstance(node, previousInstance);
            var previouses = workflowInstance.TryGetAllPrevious(node);

            // Some branches have yet to reach this node, the last one arriving resumes it.
            if (previouses == null)
            {
                progress?.StateChanges?.Report(instance);
                return [];
            }

            instance.Parameters = node.ResolveInputMapping(workflowInstance.Execution.GetWaitedInstanceContextFor(previouses));
        }
        else
        {
            instance = workflowInstance.CreateInstance(
                node,
                node.ResolveInputMapping(workflowInstance.Execution.GetInstanceContextFor(previousInstance)),
                EnumTaskState.Progressing,
                previousInstance);
        }

        if (node.IsShare())
            workflowInstance.SharedContext = GraphContext.Merge(workflowInstance.SharedContext, instance.Parameters);

        // A control produces nothing of its own, it hands over its resolved parameters.
        instance.Output = instance.Parameters ?? new JObject();
        instance.State = EnumTaskState.Completed;
        progress?.StateChanges?.Report(instance);

        // The end closes the branch, its instance is the result of the workflow.
        if (node.IsEnd())
        {
            if (workflowInstance.Workflow.WorkflowSettings.StopAtFirstEnd)
                workflowInstance.WorkflowCts.Cancel();
            return [instance];
        }

        return await NextAsync(node, instance, workflowInstance, progress, cancellation);
    }
    #endregion
}
