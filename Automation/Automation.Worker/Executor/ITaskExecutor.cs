using Automation.Models.Work;
using Automation.Shared.Data.Task;

namespace Automation.Worker.Executor;

public interface ITaskExecutor
{
    Task<TaskInstance> ExecuteAsync(
        TaskInstance instance,
        IProgress<TaskInstanceNotification>? progress = null);
}