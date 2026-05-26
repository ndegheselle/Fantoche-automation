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
    public BaseGraphTask? GraphNode { get; set; }
    public Guid? WorkflowInstanceId { get; set; }
    /// <summary>
    /// Pre-created Waiting instance to adopt instead of creating a new one (wait-all-inputs nodes).
    /// </summary>
    public NodeInstance? ExistingInstance { get; set; }
    /// <summary>
    /// Predecessor instances used for Previous/Nexts linking and context building.
    /// </summary>
    public IReadOnlyList<NodeInstance>? PreviousInstances { get; set; }
    /// <summary>
    /// Called whenever the node instance is created or its state changes.
    /// </summary>
    public Action<NodeInstance>? OnInstanceChange { get; set; }
}

public readonly struct NodeTaskOutput
{
    public TaskOutput Output { get; init; }
    public NodeInstance? Instance { get; init; }
}

/// <summary>
/// Execute a task locally.
/// </summary>
public class LocalTaskExecutor : ITaskExecutor, IDisposable
{
    private readonly IPackageManagement _packages;
    private readonly LocalWorkflowExecutor _workflowExecutor;
    /// <summary>
    /// Task loaders cached by DLL path.
    /// </summary>
    private readonly Dictionary<string, TaskLoader> _cachedTaskLoaders = [];

    public LocalTaskExecutor(IPackageManagement packageManagement, WorkflowChanges? changes)
    {
        _workflowExecutor = new LocalWorkflowExecutor(this, changes);
        _packages = packageManagement;
    }

    public void Dispose()
    {
        foreach (var loader in _cachedTaskLoaders)
            loader.Value.Dispose();
    }

    public async Task<TaskOutput> ExecuteAsync(
        BaseAutomationTask automationTask,
        JToken? input,
        IProgress<TaskNotification>? notifications = null,
        CancellationToken? cancellation = null)
    {
        var result = await ExecuteAsync(
            automationTask,
            new LocalExecutionContext { Input = input },
            notifications,
            cancellation);
        return result.Output;
    }

    public async Task<NodeTaskOutput> ExecuteAsync(
        BaseAutomationTask automationTask,
        LocalExecutionContext context,
        IProgress<TaskNotification>? notifications = null,
        CancellationToken? cancellation = null)
    {
        NodeInstance? instance = null;

        if (context.GraphNode != null)
        {
            instance = context.ExistingInstance ?? new NodeInstance
            {
                GraphNodeId = context.GraphNode.Id,
                WorkflowInstanceId = context.WorkflowInstanceId,
                TaskId = context.GraphNode.TaskId,
                Name = context.GraphNode.Name,
            };

            if (context.PreviousInstances != null)
                foreach (var prev in context.PreviousInstances)
                    instance.Link(prev);

            instance.Input = context.Input;
            instance.State = EnumTaskState.Progressing;
            context.OnInstanceChange?.Invoke(instance);
        }

        TaskOutput output = new TaskOutput();
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

            output = automationTask switch
            {
                AutomationTask task => await ExecuteTaskAsync(task, context, notifications, cancellation),
                AutomationWorkflow workflow => await ExecuteWorkflowAsync(workflow, context, notifications, cancellation),
                _ => throw new Exception("Unknown task type.")
            };
        }
        catch (OperationCanceledException)
        {
            output = new TaskOutput { State = EnumTaskState.Canceled };
        }
        catch (Exception ex)
        {
            output = new TaskOutput { State = EnumTaskState.Failed, OutputToken = JToken.FromObject(ex.ToString()) };
        }

        if (instance != null)
        {
            instance.Output = output.OutputToken;
            instance.State = output.State;
            if ((output.State & EnumTaskState.Finished) != 0)
                instance.FinishedAt = DateTime.UtcNow;
            context.OnInstanceChange?.Invoke(instance);
        }

        return new NodeTaskOutput { Output = output, Instance = instance };
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

        if (task is ITaskControl && context.GraphNode != null)
            parameter = new ControlContext(context.GraphNode, parameter);

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
        return _workflowExecutor.ExecuteAsync(automationWorkflow, context.Input, null, progress, cancellation);
    }
}
