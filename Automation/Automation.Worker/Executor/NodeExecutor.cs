using Automation.Plugins.Shared;
using Automation.Shared.Data;
using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Scoped;
using Automation.Worker.Packages;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;

namespace Automation.Worker.Executor;

public class NodeExecutionException : Exception
{
    public NodeExecutionException(string message) : base(message) { }
}

/// <summary>
/// Handle the concrete execution of a task
/// </summary>
public class NodeExecutor : IDisposable
{
    private readonly LocalPackageManagement _packages;
    private readonly WorkflowExecutor _workflowExecutor;
    /// <summary>
    /// Task loaders cached by DLL path.
    /// </summary>
    private readonly Dictionary<string, TaskLoader> _cachedTaskLoaders = [];

    public NodeExecutor(LocalPackageManagement packageManagement, WorkflowExecutor workflowExecutor)
    {
        _workflowExecutor = workflowExecutor;
        _packages = packageManagement;
    }

    public void Dispose()
    {
        foreach (var loader in _cachedTaskLoaders)
            loader.Value.Dispose();
    }

    public async Task<TaskInstance> ExecuteAsync(
        BaseAutomationTask automationTask,
        TaskInstance instance,
        TaskInstancesProgress? progress = null,
        CancellationToken? cancellation = null)
    {
        try
        {
            if (instance.Parameters == null)
            {
                // Pass-through tasks may legitimately run with no parameters (no template),
                // since they don't transform anything — only the context walks past them.
                if (automationTask.InputSchema != null && automationTask.Settings.IsPassingThrough == false)
                    throw new NodeExecutionException("Parameters are required for this task.");
            }
            else
            {
                var errors = automationTask.InputSchema?.Validate(instance.Parameters);
                if (errors?.Count > 0)
                    throw new NodeExecutionException($"Parameters don't correspond to schema : {string.Join(", ", errors)}");
            }

            instance = automationTask switch
            {
                AutomationWorkflow workflow => await ExecuteWorkflowAsync(
                    workflow, 
                    instance as WorkflowInstance ?? throw new NodeExecutionException("A workflow task must have a workflow instance"), 
                    progress, cancellation),
                AutomationControl control => await ExecuteControlAsync(control, instance, progress, cancellation),
                AutomationTask task => await ExecuteTaskAsync(task, instance, progress, cancellation),
                _ => throw new NodeExecutionException("Unknown task type.")
            };
        }
        catch (OperationCanceledException)
        {
            instance.State = EnumTaskState.Canceled;
        }
        catch (Exception ex)
        {
            instance.State = EnumTaskState.Failed;
            instance.Output = JToken.FromObject(ex.ToString());
        }

        return instance;
    }

    private async Task<TaskInstance> ExecuteWorkflowAsync(
        AutomationWorkflow automationWorkflow,
        WorkflowInstance instance,
        TaskInstancesProgress? progress = null,
        CancellationToken? cancellation = null)
    {
        instance = await _workflowExecutor.ExecuteAsync(
            instance,
            progress,
            cancellation);
        instance.State = EnumTaskState.Completed;
        return instance;
    }

    private async Task<TaskInstance> ExecuteTaskAsync(
        AutomationTask automationTask,
        TaskInstance instance,
        TaskInstancesProgress? progress = null,
        CancellationToken? cancellation = null)
    {
        if (automationTask.Target is not PackageClassTarget target)
            throw new NodeExecutionException("Task target is not a package.");

        string dllPath =
            await _packages.DownloadToLocalIfMissing(target.Package.Id, target.Package.Version, target.Dll);

        TaskLoader loader;
        if (_cachedTaskLoaders.TryGetValue(dllPath, out TaskLoader? cached))
            loader = cached;
        else
        {
            loader = new TaskLoader(dllPath);
            _cachedTaskLoaders.Add(dllPath, loader);
        }

        var task = loader.CreateInstance(target.ClassFullName);

        // Pass-through with no parameters: nothing to deserialize, nothing for the plugin
        // to do — short-circuit before forcing a TInput-typed conversion that would throw.
        if (task.IsPassThrough() && instance.Parameters == null)
            return instance;

        object? parameter = null;
        if (instance.Parameters != null && task.Input?.Type != null)
            parameter = instance.Parameters.ToObject(task.Input.Type);

        var runtime = new TaskRuntime(progress?.Notifications);
        var result = await task.DoAsync(parameter, runtime, cancellation);

        if (result != null)
            instance.Output = JToken.FromObject(result);
        else
            instance.Output = new JObject();

        if (runtime.IsOutputDeactivated)
            instance.Output = null;

        instance.State = EnumTaskState.Completed;
        return instance;
    }

    private async Task<TaskInstance> ExecuteControlAsync(
        AutomationControl automationControl,
        TaskInstance instance,
        TaskInstancesProgress? progress = null,
        CancellationToken? cancellation = null)
    {
        if (automationControl.Id == AutomationControl.ShareTask.Id)
            instance.State = ShareControl(automationControl, instance, progress, cancellation);
        else if (automationControl.Id == AutomationControl.JoinTask.Id)
            instance.State = JoinControl(automationControl, instance, progress, cancellation);
        return instance;
    }

    #region Control tasks
    private EnumTaskState JoinControl(
        AutomationControl automationControl,
        TaskInstance instance,
        TaskInstancesProgress? progress = null,
        CancellationToken? cancellation = null)
    {
        if (instance.ParentWorkflow == null)
            throw new NodeExecutionException("A control task instance need a parent workflow to execute.");
        if (instance.Node == null)
            throw new NodeExecutionException("A control task instance need a link to the node object.");
        if (instance.Previous == null)
            throw new NodeExecutionException("A control task instance need a link to the previous instance.");

        instance.ParentWorkflow.GetOrCreateWaitingInstance(instance.Node, instance.Previous);
        var previousInstances = instance.ParentWorkflow.TryGetAllPrevious(instance.Node);

        // All previous are not ready yet
        if (previousInstances == null || previousInstances.Count == 0)
            return EnumTaskState.Waiting;

        instance.Output = instance.ParentWorkflow.Execution.GetInstanceContextFor(instance.Node, instance.Previous);
        return EnumTaskState.Completed;
    }

    private EnumTaskState ShareControl(
        AutomationControl automationControl,
        TaskInstance instance,
        TaskInstancesProgress? progress = null,
        CancellationToken? cancellation = null)
    {
        if (instance.ParentWorkflow == null)
            throw new NodeExecutionException("A control task instance need a parent workflow to execute.");

        instance.ParentWorkflow.SharedContext = GraphExecutionContext.MergeContexts(instance.ParentWorkflow.SharedContext, instance.Parameters);
        // XXX : maybe report a specific event ?
        progress?.StateChanges?.Report(instance.ParentWorkflow);
        return EnumTaskState.Completed;
    }

    private EnumTaskState EndControl(
        AutomationControl automationControl,
        TaskInstance instance,
        TaskInstancesProgress? progress = null,
        CancellationToken? cancellation = null)
    {
        if (instance.ParentWorkflow == null)
            throw new NodeExecutionException("A control task instance need a parent workflow to execute.");

        if (instance.ParentWorkflow.Workflow.WorkflowSettings.StopIfAnyTaskFail)
        {

        }
        else
        {
            // Treat the end as a 
            return JoinControl(automationControl, instance, progress, cancellation);
        }

        return EnumTaskState.Completed;
    }
    #endregion
}
