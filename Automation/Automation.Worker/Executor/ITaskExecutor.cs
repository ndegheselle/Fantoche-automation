using Automation.Models.Work;
using Automation.Plugins.Shared;
using Automation.Shared.Data.Task;
using Automation.Worker.Control;
using Newtonsoft.Json.Linq;

namespace Automation.Worker.Executor;

public record TaskInput
{
    public JToken? InputToken { get; set; }
    public JToken? ContextToken { get; set; }
}

public record TaskOutput
{
    public JToken? OutputToken { get; set; }
    public EnumTaskState State { get; set; }
}

public interface ITaskExecutor
{
    Task<TaskOutput> ExecuteAsync(
        BaseAutomationTask automationTask,
        TaskInput input,
        IProgress<TaskNotification>? notifications = null,
        CancellationToken? cancellation = null);
}

public interface ITaskChangeHandler
{
    void OnTaskStart(
        BaseAutomationTask automationTask,
        TaskInput input,
        WorkflowContext? workflowContext);
    void OnTaskEnd(
        BaseAutomationTask automationTask,
        TaskOutput output,
        WorkflowContext? workflowContext);
}