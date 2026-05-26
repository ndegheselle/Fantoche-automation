using Automation.Plugins.Control;
using Automation.Plugins.Shared;
using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Graph;
using Automation.Shared.Data.Scoped;
using Automation.Worker.Packages;
using Newtonsoft.Json.Linq;

namespace Automation.Worker.Executor;

public class LocalExecutionContext
{
    public JToken? Input { get; set; }
    public JToken? Shared { get; set; }
    public BaseGraphTask? CurrentNode { get; set; }
}

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

    public Task<TaskOutput> ExecuteAsync(
        BaseAutomationTask automationTask,
        JToken? input,
        IProgress<TaskNotification>? notifications = null,
        CancellationToken? cancellation = null)
    {
        return ExecuteAsync(
            automationTask,
            new LocalExecutionContext { Input = input },
            notifications,
            cancellation);
    }

    public async Task<TaskOutput> ExecuteAsync(
        BaseAutomationTask automationTask,
        LocalExecutionContext context,
        IProgress<TaskNotification>? notifications = null,
        CancellationToken? cancellation = null)
    {
        try
        {
            if (context.Input == null)
            {
                if (automationTask.InputSchema != null)
                    throw new Exception("Input is required for this task.");
            }
            else
            {
                var errors = automationTask.InputSchema?.Validate(context.Input);
                if (errors?.Count > 0)
                    throw new Exception($"Input doesn't correspond to schema : {string.Join(", ", errors)}");
            }

            return automationTask switch
            {
                AutomationTask task => await ExecuteTaskAsync(task, context, notifications, cancellation),
                AutomationWorkflow workflow => await ExecuteWorkflowAsync(workflow, context, notifications, cancellation),
                _ => throw new Exception("Unknown task type.")
            };
        }
        catch (OperationCanceledException)
        {
            return new TaskOutput { State = EnumTaskState.Canceled };
        }
        catch (Exception ex)
        {
            return new TaskOutput { State = EnumTaskState.Failed, OutputToken = JToken.FromObject(ex.ToString()) };
        }
    }

    private async Task<TaskOutput> ExecuteTaskAsync(
        AutomationTask automationTask,
        LocalExecutionContext context,
        IProgress<TaskNotification>? notifications = null,
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
        if (context.Input != null && task.Input?.Type != null)
            parameter = context.Input.ToObject(task.Input.Type);

        if (task is ITaskControl && context.CurrentNode != null)
            parameter = new ControlContext(context.CurrentNode, parameter);

        var result = await task.DoAsync(parameter, notifications, cancellation);

        TaskOutput output = new TaskOutput { State = EnumTaskState.Completed };
        if (result is ControlOutput controlOutput)
        {
            if (controlOutput.Result != null)
                output.OutputToken = JToken.FromObject(controlOutput.Result);
            output.ActiveOutputConnectorIds = controlOutput.ActiveOutputConnectorIds;
        }
        else if (result != null)
        {
            output.OutputToken = JToken.FromObject(result);
        }

        return output;
    }

    private Task<TaskOutput> ExecuteWorkflowAsync(
        AutomationWorkflow automationWorkflow,
        LocalExecutionContext context,
        IProgress<TaskNotification>? progress = null,
        CancellationToken? cancellation = null)
    {
        return _workflowExecutor.ExecuteAsync(automationWorkflow, context.Input, context.Shared, progress, cancellation);
    }
}
