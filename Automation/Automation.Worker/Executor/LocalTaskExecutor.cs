using Automation.Dal;
using Automation.Dal.Repositories;
using Automation.Models.Work;
using Automation.Plugins.Shared;
using Automation.Shared.Data.Task;
using Automation.Worker.Control;
using Automation.Worker.Packages;

namespace Automation.Worker.Executor;

/// <summary>
/// Execute a task localy
/// </summary>
public class LocalTaskExecutor : ITaskExecutor
{
    private readonly IPackageManagement _packages;

    public LocalTaskExecutor(IPackageManagement packageManagement)
    {
        _packages = packageManagement;
    }

    public Task<TaskInstance> ExecuteAsync(
        TaskInstance instance,
        IProgress<TaskInstanceState>? states = null,
        IProgress<TaskInstanceNotification>? notifications = null,
        CancellationToken? cancellation = null)
    {
        return ExecuteAsync(instance, null, states, notifications, cancellation);
    }

    public async Task<TaskInstance> ExecuteAsync(
        TaskInstance instance,
        WorkflowContext? context,
        IProgress<TaskInstanceState>? states = null,
        IProgress<TaskInstanceNotification>? notifications = null,
        CancellationToken? cancellation = null)
    {
        if (instance.Data == null)
        {
            if (baseTask.InputSchema != null)
                throw new Exception("The input data doesn't correspond to the task input schema.");
        }
        else
        {
            var errors = baseTask.InputSchema?.Validate(instance.Data?.InputToken);
            if (errors?.Count > 0)
                throw new Exception(string.Join('\n', errors));
        }

        instance.StartedAt = DateTime.UtcNow;
        instance.State = EnumTaskState.Progressing;

        states?.Report(new TaskInstanceState(instance.Id, instance.State) { WorkflowInstanceId = context?.Workflow.Id });

        try
        {
            instance = baseTask switch
            {
                AutomationControl control => context == null
                    ? throw new Exception("Control task need the workflow context for execution.")
                    : await ExecuteControlAsync(control, context, instance, notifications, cancellation),
                AutomationTask task => await ExecuteTaskAsync(task, instance, notifications, cancellation),
                AutomationWorkflow workflow => await ExecuteWorkflowAsync(workflow, instance, notifications, cancellation),
                _ => throw new Exception("Unknown task type.")
            };
        }
        catch
        {
            instance.State = EnumTaskState.Failed;
        }
        finally
        {
            instance.FinishedAt = DateTime.UtcNow;
        }

        states?.Report(new TaskInstanceState(instance.Id, instance.State) { WorkflowInstanceId = context?.Workflow.Id });

        return instance;
    }

    private async Task<TaskInstance> ExecuteControlAsync(AutomationControl automationControl, WorkflowContext context,
        TaskInstance instance,
        IProgress<TaskInstanceNotification>? progress = null,
        CancellationToken? cancellation = null)
    {

        var controlType = ControlTasks.AvailablesById[automationControl.Id].Type;
        object typeInstance = Activator.CreateInstance(controlType) ??
                              throw new Exception($"Could not create a control instance of [{controlType}].");
        var control = (ITaskControl)typeInstance;

        IProgress<TaskNotification>? taskProgress = progress == null
            ? null
            : new Progress<TaskNotification>(notification =>
                progress.Report(new TaskInstanceNotification(instance.Id, notification))
            );
        instance.State = await control.DoAsync(context, taskProgress, cancellation);
        return instance;
    }

    private async Task<TaskInstance> ExecuteTaskAsync(
        AutomationTask automationTask,
        TaskInstance instance,
        IProgress<TaskInstanceNotification>? progress = null,
        CancellationToken? cancellation = null)
    {
        if (automationTask.Target is not PackageClassTarget target)
            throw new Exception("Task target is not a package.");

        string dllPath =
            await _packages.DownloadToLocalIfMissing(target.Package.Identifier, target.Package.Version);
        using var loader = new TaskLoader(dllPath);
        var task = loader.CreateInstance(target.TargetClass.Name);

        IProgress<TaskNotification>? taskProgress = progress == null
            ? null
            : new Progress<TaskNotification>(notification =>
                progress.Report(new TaskInstanceNotification(instance.Id, notification))
            );
        await task.DoAsync(null, taskProgress, cancellation);
        instance.State = EnumTaskState.Completed;

        return instance;
    }

    private async Task<TaskInstance> ExecuteWorkflowAsync(
        AutomationWorkflow automationWorkflow,
        TaskInstance instance,
        IProgress<TaskInstanceNotification>? progress = null,
        CancellationToken? cancellation = null)
    {
        // TODO : set instance state
        automationWorkflow.Graph.Refresh();

        var executor = new LocalWorkflowExecutor(this, automationWorkflow);
        await executor.ExecuteAsync(instance, cancellation: cancellation);
        instance.State = EnumTaskState.Completed;
        return instance;
    }
}