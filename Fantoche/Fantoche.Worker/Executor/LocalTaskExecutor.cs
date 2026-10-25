using Fantoche.Plugins.Shared;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Scoped;
using Fantoche.Worker.Packages;
using Fantoche.Worker.UserInterfaces;
using Newtonsoft.Json.Linq;

namespace Fantoche.Worker.Executor;

public class ExecutionException : Exception
{
    public ExecutionException(string message) : base(message) { }
}

/// <summary>
/// Run a task in this process : load its package and execute it, a workflow being walked by a
/// <see cref="WorkflowExecutor"/>. Built for an execution, with its storage. A task asking the user
/// runs here only where its page can be shown, see <see cref="UserTasks"/>.
/// </summary>
public class LocalTaskExecutor : ITaskExecutor
{
    private readonly TaskLoaders _tasks;
    private readonly IStorage _storage;
    private readonly UserTasks _users;
    private readonly Guid _runId;
    private readonly WorkflowExecutor _workflowExecutor;

    /// <param name="users">Where the tasks asking the user run.</param>
    /// <param name="runId">The run executed, see <see cref="PageRequest.RunId"/>.</param>
    /// <param name="workflowExecutor">
    /// What the workflows are walked with, by default one running their nodes here as well.
    /// </param>
    public LocalTaskExecutor(TaskLoaders tasks, IStorage storage, UserTasks users, Guid runId = default, WorkflowExecutor? workflowExecutor = null)
    {
        _workflowExecutor = workflowExecutor ?? new WorkflowExecutor(this);
        _tasks = tasks;
        _storage = storage;
        _users = users;
        _runId = runId;
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
                if (automationTask.InputSchema != null)
                    throw new ExecutionException("Parameters are required for this task.");
            }
            else
            {
                var errors = automationTask.InputSchema?.Validate(instance.Parameters);
                if (errors?.Count > 0)
                    throw new ExecutionException($"Parameters don't correspond to schema : {string.Join(", ", errors)}");
            }

            instance = automationTask switch
            {
                AutomationWorkflow workflow => await ExecuteWorkflowAsync(
                    workflow,
                    instance as WorkflowInstance ?? throw new ExecutionException("A workflow task must have a workflow instance"),
                    progress, cancellation),
                AutomationTask task => await ExecuteTaskAsync(task, instance, progress, cancellation),
                _ => throw new ExecutionException("Unknown task type.")
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
        // Completed by its end, or paused.
        return await _workflowExecutor.ExecuteAsync(
            instance,
            progress,
            cancellation);
    }

    private async Task<TaskInstance> ExecuteTaskAsync(
        AutomationTask automationTask,
        TaskInstance instance,
        TaskInstancesProgress? progress = null,
        CancellationToken? cancellation = null)
    {
        if (automationTask.Target is not PackageClassTarget target)
            throw new ExecutionException("Task target is not a package.");

        // Asking the user, it runs where the user is : a worker hands it to an application.
        bool asksUser = UserInterfaceSwitch.IsOn(automationTask, instance.Parameters);
        if (asksUser && _users.Pages == null)
        {
            ITaskExecutor applications = _users.Applications
                ?? throw new ExecutionException($"'{instance.NodeName}' asks the user, and nothing here shows its page.");
            return await applications.ExecuteAsync(automationTask, instance, progress, cancellation);
        }

        var task = await _tasks.CreateAsync(target);

        // Pass-through with no parameters: nothing to deserialize, nothing for the plugin
        // to do — short-circuit before forcing a TInput-typed conversion that would throw.
        if (task.IsPassThrough() && instance.Parameters == null)
            return instance;

        object? parameter = null;
        if (instance.Parameters != null && task.Input?.Type != null)
            parameter = instance.Parameters.ToObject(task.Input.Type);

        PageSession? page = asksUser ? await PageOfAsync(task, target, instance, progress) : null;
        var runtime = new TaskRuntime(progress?.Notifications, _storage, page);
        page?.Bind(runtime);
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

    /// <summary>The page [task] asks the user through, shown by the pages of the application.</summary>
    private async Task<PageSession> PageOfAsync(ITask task, PackageClassTarget target, TaskInstance instance, TaskInstancesProgress? progress)
    {
        if (task is not IUserInterfaceTask)
            throw new ExecutionException($"'{instance.NodeName}' asks the user, but its task has no page.");

        string folder = await _tasks.GetPageFolderAsync(target);
        return new PageSession(_users.Pages!, folder, task, instance, _runId, _storage, progress?.StateChanges);
    }
}
