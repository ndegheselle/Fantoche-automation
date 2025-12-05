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
    private readonly TasksRepository _taskRepo;
    private readonly TaskInstancesRepository _taskInstances;
    private readonly IPackageManagement _packages;
    private readonly DatabaseConnection _connection;

    public LocalTaskExecutor(DatabaseConnection connection, IPackageManagement packageManagement)
    {
        _connection = connection;
        _taskRepo = new TasksRepository(connection);
        _taskInstances = new TaskInstancesRepository(connection);
        _packages = packageManagement;
    }

    public Task<TaskInstance> ExecuteAsync(
        TaskInstance instance,
        IProgress<TaskInstanceNotification>? progress = null,
        CancellationToken? cancellation = null)
    {
        return ExecuteAsync(instance, null, progress, cancellation);
    }

    public async Task<TaskInstance> ExecuteAsync(
        TaskInstance instance,
        WorkflowContext? context,
        IProgress<TaskInstanceNotification>? progress = null,
        CancellationToken? cancellation = null)
    {
        var baseTask = await _taskRepo.GetByIdAsync(instance.TaskId);

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

        _ = _taskInstances.CreateAsync(instance);

        var result = baseTask switch
        {
            AutomationControl control => context == null
                ? throw new Exception("Control task need the workflow context for execution.")
                : await ExecuteControlAsync(control, context, instance, progress, cancellation),
            AutomationTask task => await ExecuteTaskAsync(task, instance, progress, cancellation),
            AutomationWorkflow workflow => await ExecuteWorkflowAsync(workflow, instance, progress, cancellation),
            _ => throw new Exception("Unknown task type.")
        };

        _ = _taskInstances.ReplaceAsync(result.Id, result);
        return result;
    }

    private async Task<TaskInstance> ExecuteControlAsync(AutomationControl automationControl, WorkflowContext context,
        TaskInstance instance,
        IProgress<TaskInstanceNotification>? progress = null,
        CancellationToken? cancellation = null)
    {
        instance.StartedAt = DateTime.UtcNow;
        try
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
            await control.DoAsync(context, taskProgress, cancellation);
            instance.State = EnumTaskState.Completed;
        }
        catch
        {
            instance.State = EnumTaskState.Failed;
        }

        instance.FinishedAt = DateTime.UtcNow;

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

        instance.StartedAt = DateTime.UtcNow;
        try
        {
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
        }
        catch
        {
            instance.State = EnumTaskState.Failed;
        }

        instance.FinishedAt = DateTime.UtcNow;

        return instance;
    }

    private async Task<TaskInstance> ExecuteWorkflowAsync(
        AutomationWorkflow automationWorkflow,
        TaskInstance instance,
        IProgress<TaskInstanceNotification>? progress = null,
        CancellationToken? cancellation = null)
    {
        var executor = new LocalWorkflowExecutor(_connection, this, automationWorkflow);
        await executor.ExecuteAsync(instance, progress, cancellation);
        return instance;
    }
}