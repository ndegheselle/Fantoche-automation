using Automation.Models.Work;
using Automation.Plugins.Shared;
using Automation.Shared.Data.Task;

namespace Automation.Worker.Control
{
    public class WorkflowContext
    {
        public AutomationWorkflow Workflow { get; }
        public WorkflowContext(AutomationWorkflow workflow)
        {
            Workflow = workflow;
        }
    }

    public interface ITaskControl : ITask
    {
        public Task<EnumTaskState> DoAsync(WorkflowContext context, IProgress<TaskNotification>? progress = null, CancellationToken? cancellation = null);
    }
}
