using Automation.Models.Work;
using Automation.Plugins.Shared;
using Automation.Shared.Data.Task;
using Newtonsoft.Json.Linq;

namespace Automation.Worker.Executor;

public class TaskInputs
{
    public JToken? InputToken { get; set; }
    public JToken? ContextToken { get; set; }
}

public class TaskOutput
{
    public JToken? OutputToken { get; set; }
    public EnumTaskState State { get; set; }
}

public interface ITaskExecutor
{
    Task<TaskOutput> ExecuteAsync(
        TaskInputs inputs,
        IProgress<TaskInstanceState>? states = null,
        IProgress<TaskInstanceNotification>? notifications = null,
        CancellationToken? cancellation = null);
}