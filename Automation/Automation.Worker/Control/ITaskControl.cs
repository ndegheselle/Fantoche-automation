using Automation.Models.Work;
using Automation.Plugins.Shared;
using Automation.Shared.Data.Task;
using Newtonsoft.Json.Linq;

namespace Automation.Worker.Control
{
    public class WorkflowContext
    {
        public AutomationWorkflow Workflow { get; }
        /// <summary>
        /// Shared context between task, initalized with workflow parent context.
        /// </summary>
        public JToken? Shared { get; }

        public WorkflowContext(AutomationWorkflow workflow)
        {
            Workflow = workflow;
            Shared = Workflow.ParentContext;
        }
    }

    public interface ITaskControl : ITask
    {
        public Task<EnumTaskState> DoAsync(WorkflowContext context, IProgress<TaskNotification>? progress = null, CancellationToken? cancellation = null);
    }
}