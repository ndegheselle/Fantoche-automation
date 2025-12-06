using Automation.Models.Work;
using Automation.Plugins.Shared;
using Automation.Shared.Data.Task;

namespace Automation.Worker.Executor;

public interface ITaskExecutor
{
    Task<TaskInstance> ExecuteAsync(
        TaskInstance instance,
        IProgress<TaskInstanceState>? states = null,
        IProgress<TaskInstanceNotification>? notifications = null,
        CancellationToken? cancellation = null);
}