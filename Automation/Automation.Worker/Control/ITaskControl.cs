using Automation.Models.Work;
using Automation.Plugins.Shared;
using Automation.Shared.Data.Execution;
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

    public class ControlOutput
    {
        public EnumTaskState State { get; set; } = EnumTaskState.Completed;
        /// <summary>
        /// When set, only these output connector IDs will be followed. null means all outputs active.
        /// </summary>
        public HashSet<Guid>? ActiveOutputConnectorIds { get; set; }
    }

    public interface ITaskControl : ITask
    {
        Task<ControlOutput> DoAsync(
            WorkflowContext context,
            BaseGraphTask currentNode,
            JToken? input,
            IProgress<TaskNotification>? progress = null,
            CancellationToken? cancellation = null);
    }

    /// <summary>
    /// Base class for control tasks. Control execution goes through ITaskControl.DoAsync,
    /// not through the plugin ITask.DoAsync path.
    /// </summary>
    public abstract class BaseControlTask : ITaskControl
    {
        public TaskConnector? Input => null;
        public TaskConnector? Output => null;

        public Task<object?> DoAsync(object? parameters, IProgress<TaskNotification>? progress = null, CancellationToken? cancellation = null)
            => throw new NotSupportedException("Control tasks are executed via ITaskControl.DoAsync().");

        public abstract Task<ControlOutput> DoAsync(
            WorkflowContext context,
            BaseGraphTask currentNode,
            JToken? input,
            IProgress<TaskNotification>? progress = null,
            CancellationToken? cancellation = null);
    }
}