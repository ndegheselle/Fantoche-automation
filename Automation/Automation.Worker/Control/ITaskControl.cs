using Automation.Plugins.Shared;
using Automation.Shared.Data.Task;

namespace Automation.Worker.Control
{
    public class WorkflowContext
    {
        // Graph
        // Current token and so on
    }

    public interface ITaskControl : ITask
    {
        public Task<EnumTaskState> DoAsync(WorkflowContext context, IProgress<TaskNotification>? progress = null);
    }
}
