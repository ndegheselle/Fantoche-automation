using Automation.Models.Work;
using Automation.Plugins.Shared;
using Automation.Shared.Data.Scoped;
using Automation.Shared.Data.Task;
using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace Automation.Worker.Control.Flow
{
    public class StartTask : BaseControlTask
    {
        public static readonly AutomationControl AutomationTask =
            new AutomationControl(typeof(StartTask))
            {
                Id = AutomationControl.StartTaskId,
                Metadata = new ScopedMetadata(EnumScopedType.Task) { Tags=["Control"], Name = "Start", Icon = "\uE3D2", IsReadOnly = true },
                InputSchema = null,
                // The workflow setting will set a custom schema
                OutputSchema = new JsonSchema(),
            };

        public override Task<ControlOutput> DoAsync(WorkflowContext context, BaseGraphTask currentNode, JToken? input, IProgress<TaskNotification>? progress = null, CancellationToken? cancellation = null)
        {
            return Task.FromResult(new ControlOutput());
        }
    }
}
