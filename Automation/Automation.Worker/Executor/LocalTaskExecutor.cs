using Automation.Plugins.Shared;
using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Graph;
using Automation.Shared.Data.Scoped;
using Automation.Worker.Packages;
using Newtonsoft.Json.Linq;

namespace Automation.Worker.Executor;

public class ExecutionContext
{
    public JToken? Input { get; set; }
    public BaseGraphTask? GraphNode { get; set; }
}

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
        return ExecuteAsync(automationTask, new ExecutionContext()
        {
            Input = input,
        }, notifications, cancellation);
    }

    public async Task<TaskOutput> ExecuteAsync(
        BaseAutomationTask automationTask,
        ExecutionContext context,
        IProgress<TaskNotification>? notifications = null,
        CancellationToken? cancellation = null)
    {
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
                    throw new Exception("Input doesn't correspond to schema.");
            }

            output = automationTask switch
            {
                AutomationTask task => await ExecuteTaskAsync(task, context, notifications, cancellation),
                AutomationWorkflow workflow => await ExecuteWorkflowAsync(workflow, context, notifications, cancellation),
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

    private async Task<TaskOutput> ExecuteTaskAsync(
        AutomationTask automationTask,
        ExecutionContext context,
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
        if (context.Input != null && task.Input?.Type != null)
            parameter = context.Input.ToObject(task.Input.Type);

        var result = await task.DoAsync(parameter, notifications, cancellation);

        TaskOutput output = new TaskOutput();
        if (result != null)
            output.OutputToken = JToken.FromObject(result);
        output.State = EnumTaskState.Completed;

        return output;
    }

    private async Task<TaskOutput> ExecuteWorkflowAsync(
        AutomationWorkflow automationWorkflow,
        ExecutionContext context,
        IProgress<TaskNotification>? progress = null,
        CancellationToken? cancellation = null)
    {
        TaskOutput output = new TaskOutput();
        var executor = new LocalWorkflowExecutor(this, _changes);
        await executor.ExecuteAsync(automationWorkflow, context, progress, cancellation);
        output.State = EnumTaskState.Completed;
        return output;
    }
}