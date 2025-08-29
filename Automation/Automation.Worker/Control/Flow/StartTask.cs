using Automation.Models;
using Automation.Shared.Data;
using Automation.Shared.Data.Task;

namespace Automation.Worker.Control.Flow
{
    public class StartTask : ITaskControl
    {
        public static readonly Shared.Data.Task.AutomationControl AutomationTask = 
            new Shared.Data.Task.AutomationControl(typeof(StartTask))
            {
                Id = Guid.Parse("00000000-0000-0000-0000-100000000002"),
                Metadata = new ScopedMetadata(EnumScopedType.Task) { Name = "Start", Icon = "\uE3D2", IsReadOnly = true },
                Inputs = []
            };

        public Task<EnumTaskState> DoAsync(WorkflowContext context)
        { return Task.FromResult(EnumTaskState.Completed); }
    }
}
