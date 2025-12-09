using Automation.Models.Work;
using Automation.Plugins.Shared;
using Automation.Shared.Data.Task;
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
        TaskInput input,
        BaseAutomationTask automationTask,
        IProgress<TaskNotification>? notifications = null,
        CancellationToken? cancellation = null);
}