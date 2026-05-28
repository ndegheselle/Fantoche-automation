using Automation.Shared.Data;
using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Graph;
using Automation.Shared.Data.Scoped;
using Automation.Worker.Packages;
using Newtonsoft.Json.Linq;

namespace Automation.Worker.Executor;

public class LocalWorkflowExecutor
{
    private readonly LocalNodeExecutor _executor;

    public LocalWorkflowExecutor(IPackageManagement packageManagement)
    {
        _executor = new LocalNodeExecutor(packageManagement, this);
    }

    public Task<TaskInstance> ExecuteAsync(
        AutomationWorkflow workflow,
        JToken? input,
        JToken? sharedToken = null,
        Guid? parentInstanceId = null,
        TaskInstancesProgress? progress = null,
        CancellationToken? cancellation = null)
    {
        if (workflow.Graph.IsRefreshed == false)
            throw new Exception("The workflow graph should be refreshed before being executed.");

        var workflowInstance = new WorkflowInstance(workflow, parentInstanceId, sharedToken)
        {
            Input = input
        };
        progress?.StateChanges?.Report(workflowInstance);

        return ExecuteAsync(workflowInstance, progress, cancellation);
    }

    public async Task<TaskInstance> ExecuteAsync(
        WorkflowInstance workflowInstance,
        TaskInstancesProgress? progress = null,
        CancellationToken? cancellation = null)
    {
        // Combine external cancellation with workflow's own CTS (used for StopAtFirstEnd)
        using var linkedCts = cancellation.HasValue
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellation.Value, workflowInstance.WorkflowCts.Token)
            : null;
        var token = (CancellationToken?)(linkedCts?.Token ?? workflowInstance.WorkflowCts.Token);

        var startTasks = new List<Task<IReadOnlyList<TaskInstance>>>();
        foreach (var start in workflowInstance.Workflow.Graph.GetStartNodes())
        {
            var startInstance = workflowInstance.CreateInstance(start, workflowInstance.Input, EnumTaskState.Completed);
            startInstance.Output = workflowInstance.Input;

            progress?.StateChanges?.Report(startInstance);
            startTasks.Add(NextAsync(start, startInstance, workflowInstance, null, progress, token));
        }

        var results = await Task.WhenAll(startTasks);
        var endInstances = results.SelectMany(r => r).ToList();
        return EndAsync(workflowInstance, endInstances, progress);
    }

    private async Task<IReadOnlyList<TaskInstance>> NextAsync(
        BaseGraphTask current,
        TaskInstance currentInstance,
        WorkflowInstance workflowInstance,
        HashSet<Guid>? activeOutputConnectorIds,
        TaskInstancesProgress? progress,
        CancellationToken? cancellation)
    {
        var nextPairs = workflowInstance.Workflow.Graph.GetNext(current);

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
                if (workflowInstance.Workflow.WorkflowSettings.StopAtFirstEnd)
                    workflowInstance.WorkflowCts.Cancel();
                continue;
            }

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
    /// Run a branch of the workflow, return the next branches instances. The next branches instances can be empty if the task fail, is canceled or doesn't have outputs connections.
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
        TaskInstance? existingInstance = null;
        IReadOnlyList<TaskInstance>? previousInstances = null;
        // If the task wait for all inputs but only have one we treat it like other tasks and skip this part
        if (node.Settings.IsWaitingAllInputs && workflowInstance.Workflow.Graph.WithMultipleInputsConnections(node))
        {
            existingInstance = workflowInstance.GetOrCreateWaitingInstance(node, previousInstance);
            previousInstances = workflowInstance.TryGetAllPrevious(node);

            // All previous are not ready yet
            if (previousInstances == null || previousInstances.Count == 0)
                return [];

            foreach (var prev in previousInstances)
                existingInstance.Link(prev);

            progress?.StateChanges?.Report(existingInstance);
        }

        previousInstances ??= [previousInstance];
        JToken? input = null;
        if (!string.IsNullOrEmpty(node.InputJson))
        {
            var taskContext = workflowInstance.Workflow.Graph.Execution.GetContextFor(node, previousInstances, workflowInstance.SharedToken);
            input = ReferencesHandler.ReplaceReferences(JToken.Parse(node.InputJson), taskContext).ReplacedSetting;
        }

        // Set the instance as progressing
        if (existingInstance == null)
            existingInstance = workflowInstance.CreateInstance(node, input, EnumTaskState.Progressing, previousInstance);
        else
        {
            existingInstance.State = EnumTaskState.Progressing;
        }

        progress?.StateChanges?.Report(existingInstance);
        var output = await _executor.ExecuteAsync(
            node.AutomationTask ?? throw new Exception("Workflow tasks are not loaded (is the graph refreshed?)."),
            existingInstance,
            progress,
            cancellation);
        progress?.StateChanges?.Report(existingInstance);

        if (output.State == EnumTaskState.Completed)
            return await NextAsync(node, existingInstance, workflowInstance, output.ActiveOutputConnectorIds, progress, cancellation);

        return [];
    }

    private TaskInstance EndAsync(WorkflowInstance workflowInstance, IReadOnlyList<TaskInstance> endInstances, TaskInstancesProgress? progress = null)
    {
        // TODO : return failed and handle task instance on the level of the workflow
        if (workflowInstance.Workflow.OutputSchema != null && endInstances.Count == 0)
            throw new Exception("Reached end of workflow without data.");

        workflowInstance.State = EnumTaskState.Completed;
        workflowInstance.Output = workflowInstance.Workflow.Graph.Execution.CombineEndOutputs(endInstances, workflowInstance.Workflow.WorkflowSettings);
        progress?.StateChanges?.Report(workflowInstance);
        return workflowInstance;
    }
}
