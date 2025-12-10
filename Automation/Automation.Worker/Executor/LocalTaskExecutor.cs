using Automation.Dal;
using Automation.Dal.Repositories;
using Automation.Models.Work;
using Automation.Plugins.Shared;
using Automation.Shared.Data.Task;
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
        TaskInput input,
        IProgress<TaskNotification>? notifications = null,
        CancellationToken? cancellation = null)
    {
        return ExecuteAsync(automationTask, input, null, notifications, cancellation);
    }

    public async Task<TaskOutput> ExecuteAsync(
        BaseAutomationTask automationTask,
        TaskInput input,
        WorkflowContext? workflowContext,
        IProgress<TaskNotification>? notifications = null,
        CancellationToken? cancellation = null)
    {
        
        if (input.InputToken == null)
        {
            if (automationTask.InputSchema != null)
                throw new Exception("The input data doesn't correspond to the task input schema.");
        }
        else
        {
            var errors = automationTask.InputSchema?.Validate(input.InputToken);
            if (errors?.Count > 0)
                throw new Exception(string.Join('\n', errors));
        }

        _changes?.OnTaskStart(automationTask, input, workflowContext);
        TaskOutput output;
        try
        {
            output = automationTask switch
            {
                AutomationControl control => workflowContext == null
                    ? throw new Exception("Control task need the workflow context for execution.")
                    : await ExecuteControlAsync(control, input, workflowContext, notifications, cancellation),
                AutomationTask task => await ExecuteTaskAsync(task, input, notifications, cancellation),
                AutomationWorkflow workflow => await ExecuteWorkflowAsync(workflow, input, notifications, cancellation),
                _ => throw new Exception("Unknown task type.")
            };
        }
        catch (Exception ex)
        {
            output = new TaskOutput();
            output.State = EnumTaskState.Failed;
            output.OutputToken = ex.ToString();
        }

        _changes?.OnTaskEnd(automationTask, output, workflowContext);
        return output;
    }

    private async Task<TaskOutput> ExecuteControlAsync(
        AutomationControl automationControl, 
        TaskInput input,
        WorkflowContext context,
        IProgress<TaskNotification>? notifications = null,
        CancellationToken? cancellation = null)
    {

        var controlType = ControlTasks.AvailablesById[automationControl.Id].Type;
        object typeInstance = Activator.CreateInstance(controlType) ??
                              throw new Exception($"Could not create a control instance of [{controlType}].");
        var control = (ITaskControl)typeInstance;

        TaskOutput output = new TaskOutput();
        output.State = await control.DoAsync(context, notifications, cancellation);
        return output;
    }

    private async Task<TaskOutput> ExecuteTaskAsync(
        AutomationTask automationTask,
        TaskInput input,
        IProgress<TaskNotification>? notifications = null,
        CancellationToken? cancellation = null)
    {
        if (automationTask.Target is not PackageClassTarget target)
            throw new Exception("Task target is not a package.");

        string dllPath =
            await _packages.DownloadToLocalIfMissing(target.Package.Identifier, target.Package.Version);
        using var loader = new TaskLoader(dllPath);
        var task = loader.CreateInstance(target.TargetClass.Name);

        object? parameter = null;
        if (input.InputToken != null && task.Input?.Type != null)
            parameter = input.InputToken.ToObject(task.Input.Type);
        
        var result = await task.DoAsync(parameter, notifications, cancellation);
        
        TaskOutput output = new TaskOutput();
        if (result != null)
            output.OutputToken = JToken.FromObject(result);
        output.State = EnumTaskState.Completed;

        return output;
    }

    private async Task<TaskOutput> ExecuteWorkflowAsync(
        AutomationWorkflow automationWorkflow,
        TaskInput input,
        IProgress<TaskNotification>? progress = null,
        CancellationToken? cancellation = null)
    {
        // TODO : set instance state
        automationWorkflow.Graph.Refresh();

        TaskOutput output = new TaskOutput();
        var executor = new LocalWorkflowExecutor(this, automationWorkflow);
        await executor.ExecuteAsync(instance, cancellation: cancellation);
        output.State = EnumTaskState.Completed;
        return output;
    }
}