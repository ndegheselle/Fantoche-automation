using Automation.Plugins.Shared;
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
    private readonly LocalPackageManagement _packages;
    private readonly LocalWorkflowExecutor _workflowExecutor;
    /// <summary>
    /// Task loaders cached by DLL path.
    /// </summary>
    private readonly Dictionary<string, TaskLoader> _cachedTaskLoaders = [];

    public LocalNodeExecutor(LocalPackageManagement packageManagement, LocalWorkflowExecutor workflowExecutor)
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
                    throw new Exception("Parameters are required for this task.");
            }
            else
            {
                var errors = automationTask.InputSchema?.Validate(instance.Parameters);
                if (errors?.Count > 0)
                    throw new Exception($"Parameters don't correspond to schema : {string.Join(", ", errors)}");
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
            instance.Parameters,
            instance.ParentWorkflow?.SharedToken,
            instance.Id,
            progress,
            cancellation);
    }
}
