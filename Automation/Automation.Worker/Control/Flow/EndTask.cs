using Automation.Models.Work;
using Automation.Plugins.Shared;
using Automation.Shared.Data;
using Automation.Shared.Data.Task;
using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace Automation.Worker.Control.Flow
{
    public class EndTask : BaseControlTask
    {
        public static readonly AutomationControl AutomationTask =
            new AutomationControl(typeof(StartTask))
            {
                Id = AutomationControl.EndTaskId,
                Metadata = new ScopedMetadata(EnumScopedType.Task) { Tags= ["Control"], Name = "End", Icon = "\uE244", IsReadOnly = true },
                InputSchema = new JsonSchema(),
                OutputSchema = null
            };

        public override Task<ControlOutput> DoAsync(WorkflowContext context, BaseGraphTask currentNode, JToken? input, IProgress<TaskNotification>? progress = null, CancellationToken? cancellation = null)
        {
            return Task.FromResult(new ControlOutput());
        }
    }
}
