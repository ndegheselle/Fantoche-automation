using Automation.Models.Work;
using Automation.Plugins.Shared;
using Automation.Shared.Data.Execution;
using Automation.Worker.Control;
using Automation.Worker.Packages;
using Newtonsoft.Json.Linq;

namespace Automation.Worker.Executor;

/// <summary>
/// Execute a task localy
/// </summary>
public class LocalTaskExecutor : ITaskExecutor
{
    private readonly IPackageManagement _packages;
    private readonly ITaskChangeHandler? _changes;

    public LocalTaskExecutor(IPackageManagement packageManagement, ITaskChangeHandler? changeHandler)
    {
        _packages = packageManagement;
        _changes = changeHandler;
    }

    public Task<TaskOutput> ExecuteAsync(
        BaseAutomationTask automationTask,
        JToken? input,
        IProgress<TaskNotification>? notifications = null,
        CancellationToken? cancellation = null)
    {
        return ExecuteAsync(automationTask, input, null, notifications, cancellation);
    }

    public async Task<TaskOutput> ExecuteAsync(
        BaseAutomationTask automationTask,
        JToken? input,
        WorkflowContext? workflowContext,
        IProgress<TaskNotification>? notifications = null,
        CancellationToken? cancellation = null)
    {
        return await ExecuteAsync(automationTask, input, workflowContext, null, notifications, cancellation);
    }

    public async Task<TaskOutput> ExecuteAsync(
        BaseAutomationTask automationTask,
        JToken? input,
        WorkflowContext? workflowContext,
        BaseGraphTask? graphNode,
        IProgress<TaskNotification>? notifications = null,
        CancellationToken? cancellation = null)
    {
        TaskOutput output = new TaskOutput();

        try
        {
            if (input == null)
            {
                if (automationTask.InputSchema != null)
                    throw new Exception("Input is required for this task.");
            }
            else
            {
                var errors = automationTask.InputSchema?.Validate(input);
                if (errors?.Count > 0)
                    throw new Exception("Input doesn't correspond to schema.");
            }

            output = automationTask switch
            {
                AutomationControl control => workflowContext == null || graphNode == null
                    ? throw new Exception("Control task need the workflow context and graph node for execution.")
                    : await ExecuteControlAsync(control, input, workflowContext, graphNode, notifications, cancellation),
                AutomationTask task => await ExecuteTaskAsync(task, input, notifications, cancellation),
                AutomationWorkflow workflow => await ExecuteWorkflowAsync(workflow, input, notifications, cancellation),
                _ => throw new Exception("Unknown task type.")
            };

        }
        catch (Exception ex)
        {
            output = new TaskOutput()
            {
                State = EnumTaskState.Failed,
                OutputToken = ex.ToString()
            };
        }

        return output;
    }

    private async Task<TaskOutput> ExecuteControlAsync(
        AutomationControl automationControl,
        JToken? input,
        WorkflowContext context,
        BaseGraphTask graphNode,
        IProgress<TaskNotification>? notifications = null,
        CancellationToken? cancellation = null)
    {
        var controlType = ControlTasks.AvailablesById[automationControl.Id].Type;
        object typeInstance = Activator.CreateInstance(controlType) ??
                              throw new Exception($"Could not create a control instance of [{controlType}].");
        var control = (ITaskControl)typeInstance;

        var controlOutput = await control.DoAsync(context, graphNode, input, notifications, cancellation);
        return new TaskOutput
        {
            State = controlOutput.State,
            ActiveOutputConnectorIds = controlOutput.ActiveOutputConnectorIds
        };
    }

    private async Task<TaskOutput> ExecuteTaskAsync(
        AutomationTask automationTask,
        JToken? input,
        IProgress<TaskNotification>? notifications = null,
        CancellationToken? cancellation = null)
    {
        if (automationTask.Target is not PackageClassTarget target)
            throw new Exception("Task target is not a package.");

        string dllPath =
            await _packages.DownloadToLocalIfMissing(target.Package.Identifier, target.Package.Version);
        using var loader = new TaskLoader(dllPath);
        var task = loader.CreateInstance(target.ClassFullName);

        object? parameter = null;
        if (input != null && task.Input?.Type != null)
            parameter = input.ToObject(task.Input.Type);

        var result = await task.DoAsync(parameter, notifications, cancellation);

        TaskOutput output = new TaskOutput();
        if (result != null)
            output.OutputToken = JToken.FromObject(result);
        output.State = EnumTaskState.Completed;

        return output;
    }

    private async Task<TaskOutput> ExecuteWorkflowAsync(
        AutomationWorkflow automationWorkflow,
        JToken? input,
        IProgress<TaskNotification>? progress = null,
        CancellationToken? cancellation = null)
    {
        TaskOutput output = new TaskOutput();
        var executor = new LocalWorkflowExecutor(this, _changes);
        await executor.ExecuteAsync(automationWorkflow, input, progress, cancellation);
        output.State = EnumTaskState.Completed;
        return output;
    }
}