using Automation.Plugins.Control;
using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Scoped;
using Automation.Worker.Packages;
using Newtonsoft.Json.Linq;

namespace Automation.Worker.Executor;

/// <summary>
/// Handle the concrete execution of a task
/// </summary>
public class LocalNodeExecutor : IDisposable
{
    private readonly IPackageManagement _packages;
    private readonly LocalWorkflowExecutor _workflowExecutor;
    /// <summary>
    /// Task loaders cached by DLL path.
    /// </summary>
    private readonly Dictionary<string, TaskLoader> _cachedTaskLoaders = [];

    public LocalNodeExecutor(IPackageManagement packageManagement, LocalWorkflowExecutor workflowExecutor)
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
            if (instance.Input == null)
            {
                if (automationTask.InputSchema != null)
                    throw new Exception("Input is required for this task.");
            }
            else
            {
                var errors = automationTask.InputSchema?.Validate(instance.Input);
                if (errors?.Count > 0)
                    throw new Exception($"Input doesn't correspond to schema : {string.Join(", ", errors)}");
            }

            instance = automationTask switch
            {
                AutomationTask task => await ExecuteTaskAsync(task, instance, progress, cancellation),
                AutomationWorkflow workflow => await ExecuteWorkflowAsync(workflow, instance, progress, cancellation),
                _ => throw new Exception("Unknown task type.")
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
            throw new Exception("Task target is not a package.");

        string dllPath =
            await _packages.DownloadToLocalIfMissing(target.Package.Identifier, target.Package.Version, target.Dll);

        TaskLoader loader;
        if (_cachedTaskLoaders.TryGetValue(dllPath, out TaskLoader? cached))
            loader = cached;
        else
        {
            loader = new TaskLoader(dllPath);
            _cachedTaskLoaders.Add(dllPath, loader);
        }

        var task = loader.CreateInstance(target.ClassFullName);

        object? parameter = null;
        if (instance.Input != null && task.Input?.Type != null)
            parameter = instance.Input.ToObject(task.Input.Type);

        if (task is ITaskControl && instance.Node != null)
            parameter = new ControlContext(instance.Node, parameter);

        var result = await task.DoAsync(parameter, progress?.Notifications, cancellation);

        if (result is ControlOutput controlOutput)
        {
            if (controlOutput.Result != null)
                instance.Output = JToken.FromObject(controlOutput.Result);
            instance.ActiveOutputConnectorIds = controlOutput.ActiveOutputConnectorIds;
        }
        else if (result != null)
        {
            instance.Output = JToken.FromObject(result);
        }

        return instance;
    }

    private Task<TaskInstance> ExecuteWorkflowAsync(
        AutomationWorkflow automationWorkflow,
        TaskInstance instance,
        TaskInstancesProgress? progress = null,
        CancellationToken? cancellation = null)
    {
        return _workflowExecutor.ExecuteAsync(
            automationWorkflow,
            instance.Input,
            instance.ParentWorkflow?.SharedToken,
            instance.Id,
            progress,
            cancellation);
    }
}
