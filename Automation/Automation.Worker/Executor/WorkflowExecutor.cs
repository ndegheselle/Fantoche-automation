using Automation.Shared.Data;
using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Graph;
using Automation.Shared.Data.Scoped;
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

        // Create start tasks instances
        var startTasks = new List<Task<IReadOnlyList<TaskInstance>>>();
        foreach (var start in workflowInstance.Workflow.Graph.GetStartNodes())
        {
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
        // Control nodes are driven by the workflow itself, they never reach the node executor.
        if (node.AutomationTask is AutomationControl control)
            return await RunControlBranchAsync(control, node, previousInstance, workflowInstance, progress, cancellation);

        var parameters = ResolveParameters(node, workflowInstance.Execution.GetInstanceContextFor(node, previousInstance));

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

    /// <summary>
    /// Resolve the parameters of a node : its template with the references replaced by the values
    /// of [context]. Null when the node has no template.
    /// </summary>
    private static JToken? ResolveParameters(BaseGraphTask node, JObject context)
    {
        if (string.IsNullOrEmpty(node.InputMappingJson))
            return null;
        return ReferencesHandler.ReplaceReferences(JToken.Parse(node.InputMappingJson), context).ReplacedSetting;
    }

    #region Control tasks
    /// <summary>
    /// Run a control node of the branch : a control has no task to execute, it only drives the
    /// workflow (merge the branches, feed the shared context, close the workflow).
    /// </summary>
    private async Task<IReadOnlyList<TaskInstance>> RunControlBranchAsync(
        AutomationControl control,
        BaseGraphTask node,
        TaskInstance previousInstance,
        WorkflowInstance workflowInstance,
        TaskInstancesProgress? progress,
        CancellationToken? cancellation)
    {
        // A join always merges every branch reaching it. An end does the same, unless the workflow
        // stops at the first end : the branch arriving first then kills the others.
        bool waitAllPrevious = control.Id == AutomationControl.JoinTask.Id
            || (control.Id == AutomationControl.EndTask.Id && !workflowInstance.Workflow.WorkflowSettings.StopAtFirstEnd);

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

            instance.Parameters = ResolveParameters(node, workflowInstance.Execution.GetWaitedInstanceContextFor(node, previouses));
        }
        else
        {
            instance = workflowInstance.CreateInstance(
                node,
                ResolveParameters(node, workflowInstance.Execution.GetInstanceContextFor(node, previousInstance)),
                EnumTaskState.Progressing,
                previousInstance);
        }

        if (control.Id == AutomationControl.ShareTask.Id)
            workflowInstance.SharedContext = GraphExecutionContext.MergeContexts(workflowInstance.SharedContext, instance.Parameters);

        // A control produces nothing of its own, it hands over its resolved parameters.
        instance.Output = instance.Parameters ?? new JObject();
        instance.State = EnumTaskState.Completed;
        progress?.StateChanges?.Report(instance);

        // The end closes the branch, its instance is the result of the workflow.
        if (control.Id == AutomationControl.EndTask.Id)
        {
            if (workflowInstance.Workflow.WorkflowSettings.StopAtFirstEnd)
                workflowInstance.WorkflowCts.Cancel();
            return [instance];
        }

        return await NextAsync(node, instance, workflowInstance, progress, cancellation);
    }
    #endregion
}
