using Automation.Shared.Data;
using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Graph;
using Automation.Shared.Data.Scoped;
using Automation.Worker.Packages;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;

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
        return EndAsync(workflowInstance, progress);
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
        JToken? parameters = null;
        if (!string.IsNullOrEmpty(node.ParametersJson))
        {
            var taskContext = workflowInstance.Execution.GetInstanceContextFor(node, previousInstance);
            parameters = ReferencesHandler.ReplaceReferences(JToken.Parse(node.ParametersJson), taskContext).ReplacedSetting;
        }

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
        // TODO : return failed and handle task instance on the level of the workflow
        if (workflowInstance.Workflow.OutputSchema != null && endInstances.Count == 0)
            throw new Exception("Reached end of workflow without data.");

        // TODO : implement

        workflowInstance.State = EnumTaskState.Completed;
        progress?.StateChanges?.Report(workflowInstance);
        return workflowInstance;
    }
}
