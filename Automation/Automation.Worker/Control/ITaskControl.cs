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
        public JToken? SharedToken { get; }
        public JToken? OutputToken { get; set; }

        public WorkflowContext(AutomationWorkflow workflow)
        {
            Workflow = workflow;
            SharedToken = Workflow.ParentContext;
        }
    }

    public interface ITaskControl : ITask
    {
        public Task<EnumTaskState> DoAsync(WorkflowContext context, IProgress<TaskNotification>? progress = null, CancellationToken? cancellation = null);
    }
}