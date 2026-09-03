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

        // Create start tasks instances. Every one of them exists before any branch is walked : a
        // node waiting on all of its branches crawls the graph to tell the dead ones from the
        // running ones, and a start without an instance yet would pass for a dead one.
        var starts = new List<(BaseGraphTask Node, TaskInstance Instance)>();
        foreach (var start in workflowInstance.Workflow.Graph.GetStartNodes())
        {
            var startInstance = workflowInstance.CreateInstance(start, workflowInstance.Parameters, EnumTaskState.Completed);
            // A start hands the parameters of the workflow over, an empty object when it has none :
            // an instance completed without any output is a branch that died (see BranchLiveness).
            startInstance.Output = workflowInstance.Parameters ?? new JObject();
            starts.Add((start, startInstance));
        }

        var startTasks = new List<Task<IReadOnlyList<TaskInstance>>>();
        foreach (var (node, instance) in starts)
        {
            progress?.StateChanges?.Report(instance);
            startTasks.Add(NextAsync(node, instance, workflowInstance, progress, token));
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

        if (BranchLiveness.HasDelivered(instance))
            return await NextAsync(node, instance, workflowInstance, progress, cancellation);

        // The branch dies here : the task closed its output (a conditional), failed or was
        // canceled. The nodes waiting for it downstream have to be told, or they would hold on a
        // branch that can't reach them anymore.
        return await SettleWaitingNodesAsync(workflowInstance, progress, cancellation);
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
        if (string.IsNullOrEmpty(node.ParametersJson))
            return null;
        return ReferencesHandler.ReplaceReferences(JToken.Parse(node.ParametersJson), context).ReplacedSetting;
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

        if (waitAllPrevious)
        {
            TaskInstance waiting = workflowInstance.GetOrCreateWaitingInstance(node, previousInstance);
            return await ResumeWaitingAsync(control, node, waiting, workflowInstance, progress, cancellation);
        }

        TaskInstance instance = workflowInstance.CreateInstance(
            node,
            ResolveParameters(node, workflowInstance.Execution.GetInstanceContextFor(node, previousInstance)),
            EnumTaskState.Progressing,
            previousInstance);

        return await RunControlAsync(control, node, instance, workflowInstance, progress, cancellation);
    }

    /// <summary>
    /// Try to run a node waiting on every branch reaching it. It holds while a branch can still
    /// deliver, and is skipped when they are all dead : its own branch then dies with them.
    /// </summary>
    private async Task<IReadOnlyList<TaskInstance>> ResumeWaitingAsync(
        AutomationControl control,
        BaseGraphTask node,
        TaskInstance instance,
        WorkflowInstance workflowInstance,
        TaskInstancesProgress? progress,
        CancellationToken? cancellation)
    {
        EnumWaitResolution resolution = workflowInstance.TryResolvePrevious(node, out var previouses);

        // Some branches have yet to reach this node, the last one arriving resumes it.
        if (resolution == EnumWaitResolution.Pending)
        {
            progress?.StateChanges?.Report(instance);
            return [];
        }

        // Two branches can resolve the node at the same time (the last one arriving and another
        // one dying) : whoever takes hold of the instance runs it, once.
        if (!workflowInstance.TryClaimWaitingInstance(
                instance,
                resolution == EnumWaitResolution.Dead ? EnumTaskState.Skipped : EnumTaskState.Progressing))
            return [];

        // Every branch reaching the node is dead : it will never run, and neither will anything
        // after it, which can in turn resolve the nodes waiting further down.
        if (resolution == EnumWaitResolution.Dead)
        {
            progress?.StateChanges?.Report(instance);
            return await SettleWaitingNodesAsync(workflowInstance, progress, cancellation);
        }

        instance.Parameters = ResolveParameters(node, workflowInstance.Execution.GetWaitedInstanceContextFor(node, previouses));
        return await RunControlAsync(control, node, instance, workflowInstance, progress, cancellation);
    }

    /// <summary>
    /// A branch just died : the nodes waiting on it can't wait for it any longer. Resume the ones
    /// only left with dead branches and with the ones that already delivered.
    /// <para>
    /// A waiting node is otherwise only ever resumed by a branch reaching it, so a branch dying
    /// after the others arrived would leave it waiting for good.
    /// </para>
    /// </summary>
    private async Task<IReadOnlyList<TaskInstance>> SettleWaitingNodesAsync(
        WorkflowInstance workflowInstance,
        TaskInstancesProgress? progress,
        CancellationToken? cancellation)
    {
        // Nothing is resumed once the run is over : the branches dying of a cancellation are not
        // dead ends of the graph, they were cut.
        if (cancellation?.IsCancellationRequested == true)
            return [];

        List<TaskInstance> endInstances = [];
        foreach (TaskInstance waiting in workflowInstance.GetWaitingInstances())
        {
            // Only the controls wait, and they wait on the graph : a waiting instance without its
            // node is one of a run that isn't driven from here.
            BaseGraphTask? node = waiting.Node;
            if (node == null || node.AutomationTask is not AutomationControl control)
                continue;

            endInstances.AddRange(await ResumeWaitingAsync(
                control, node, waiting, workflowInstance, progress, cancellation));
        }

        return endInstances;
    }

    /// <summary>
    /// Run the body of a control : it produces nothing of its own, it hands its resolved
    /// parameters over (feeding the shared context, closing the workflow).
    /// </summary>
    private async Task<IReadOnlyList<TaskInstance>> RunControlAsync(
        AutomationControl control,
        BaseGraphTask node,
        TaskInstance instance,
        WorkflowInstance workflowInstance,
        TaskInstancesProgress? progress,
        CancellationToken? cancellation)
    {
        if (control.Id == AutomationControl.ShareTask.Id)
            workflowInstance.SharedContext = GraphExecutionContext.MergeContexts(workflowInstance.SharedContext, instance.Parameters);

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
