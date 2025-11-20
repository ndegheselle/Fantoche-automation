using Automation.Dal;
using Automation.Dal.Repositories;
using Automation.Models.Work;
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
    private readonly IPackageManagement _packages;
    private readonly DatabaseConnection _connection;

    public LocalTaskExecutor(DatabaseConnection connection, IPackageManagement packageManagement)
    {
        _connection = connection;
        _taskRepo = new TasksRepository(connection);
        _packages = packageManagement;
    }

    public async Task<TaskInstance> ExecuteAsync(
        TaskInstance instance,
        IProgress<TaskInstanceNotification>? progress = null)
    {
        var baseTask = await _taskRepo.GetByIdAsync(instance.TaskId);
        return baseTask switch
        {
            AutomationControl => throw new Exception("A control task can be started only from a workflow."),
            AutomationTask task => await ExecuteTaskAsync(task, instance, progress),
            AutomationWorkflow workflow => await ExecuteWorkflowAsync(workflow, instance, progress),
            _ => throw new Exception("Unknown task type.")
        };
    }
    
    private async Task<TaskInstance> ExecuteTaskAsync(
        AutomationTask automationTask,
        TaskInstance instance,
        IProgress<TaskInstanceNotification>? progress = null)
    {
        if (automationTask.Target is not PackageClassTarget target)
            throw new Exception("Task target is not a package.");

        instance.StartedAt = DateTime.UtcNow;
        try
        {
            string dllPath = await _packages.DownloadToLocalIfMissing(target.Package.Identifier, target.Package.Version);
            using var loader = new TaskLoader(dllPath);
            var task = loader.CreateInstance(target.TargetClass.Name);
            await task.DoAsync(null);
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
        IProgress<TaskInstanceNotification>? progress = null)
    {
        LocalWorkflowExecutor executor = new LocalWorkflowExecutor(_connection, this, automationWorkflow);
        await executor.ExecuteAsync(instance);
        return instance;
    }
}