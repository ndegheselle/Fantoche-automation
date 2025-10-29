using Automation.Models.Work;
using Automation.Plugins.Shared;
using Automation.Shared.Data;
using Automation.Shared.Data.Task;
using NJsonSchema;

namespace Automation.Worker.Control.Flow
{
    public class StartTask : BaseTask<WorkflowContext, EnumTaskState>, ITaskControl
    {
        public static readonly AutomationControl AutomationTask =
            new AutomationControl(typeof(StartTask))
            {
                Id = AutomationControl.StartTaskId,
                Metadata = new ScopedMetadata(EnumScopedType.Task) { Name = "Start", Icon = "\uE3D2", IsReadOnly = true },
                InputSchema = null,
                // The workflow setting will set a custom schema
                OutputSchema = new JsonSchema()
            };

        public override Task<EnumTaskState> DoAsync(WorkflowContext parameters, IProgress<TaskNotification>? progress = null)
        {
            return Task.FromResult(EnumTaskState.Completed);
        }
    }
}
