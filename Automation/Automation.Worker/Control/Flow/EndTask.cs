using Automation.Models.Work;
using Automation.Plugins.Shared;
using Automation.Shared.Data;
using Automation.Shared.Data.Task;
using NJsonSchema;

namespace Automation.Worker.Control.Flow
{
    public class EndTask : BaseTask<WorkflowContext, EnumTaskState>, ITaskControl
    {
        public static readonly AutomationControl AutomationTask =
            new AutomationControl(typeof(StartTask))
            {
                Id = AutomationControl.EndTaskId,
                Metadata = new ScopedMetadata(EnumScopedType.Task) { Tags= ["Control"], Name = "End", Icon = "\uE244", IsReadOnly = true },
                InputSchema = new JsonSchema(),
                OutputSchema = null
            };

        public override Task<EnumTaskState> DoAsync(WorkflowContext parameters, IProgress<TaskNotification>? progress = null)
        {
            return Task.FromResult(EnumTaskState.Completed);
        }
    }
}
