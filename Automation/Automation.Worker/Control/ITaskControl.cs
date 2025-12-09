using Automation.Models.Work;
using Automation.Plugins.Shared;
using Automation.Shared.Data.Task;
using Automation.Worker.Executor;

namespace Automation.Worker.Control
{
    public class WorkflowContext
    {
        public AutomationWorkflow Workflow { get; }
        public TaskInstance WorkflowInstance { get; }
        
        public WorkflowContext(AutomationWorkflow workflow, TaskInstance workflowInstance)
        {
            Workflow = workflow;
            WorkflowInstance = workflowInstance;
        }
    }

    public interface ITaskControl : ITask
    {
        public Task<EnumTaskState> DoAsync(WorkflowContext context, IProgress<TaskNotification>? progress = null, CancellationToken? cancellation = null);
    }
}
