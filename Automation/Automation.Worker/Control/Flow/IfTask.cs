using Automation.Models.Work;
using Automation.Plugins.Shared;
using Automation.Shared.Data.Scoped;
using Automation.Shared.Data.Task;
using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace Automation.Worker.Control.Flow
{
    /// <summary>
    /// Conditional branch control task.
    /// The graph node must have exactly 2 output connectors: index 0 = true branch, index 1 = false branch.
    /// The input must resolve to a boolean value that selects which branch to follow.
    /// </summary>
    public class IfTask : BaseControlTask
    {
        public static readonly Guid IfTaskId = Guid.Parse("00000000-0000-0000-0000-100000000003");

        public static readonly AutomationControl AutomationTask =
            new AutomationControl(typeof(IfTask))
            {
                Id = IfTaskId,
                Metadata = new ScopedMetadata(EnumScopedType.Task)
                {
                    Tags = ["Control"],
                    Name = "If",
                    Icon = "",
                    IsReadOnly = true
                },
                InputSchema = JsonSchema.FromType<bool>(),
                OutputSchema = null,
            };

        public override Task<ControlOutput> DoAsync(
            WorkflowContext context,
            BaseGraphTask currentNode,
            JToken? input,
            IProgress<TaskNotification>? progress = null,
            CancellationToken? cancellation = null)
        {
            if (currentNode.Outputs.Count < 2)
                throw new Exception("If node must have exactly 2 output connectors (true branch at index 0, false branch at index 1).");

            bool condition = input?.ToObject<bool>() ?? false;
            var activeConnector = condition ? currentNode.Outputs[0] : currentNode.Outputs[1];

            return Task.FromResult(new ControlOutput
            {
                ActiveOutputConnectorIds = [activeConnector.Id]
            });
        }
    }
}
