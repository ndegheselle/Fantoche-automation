using Automation.Dal.Models;
using Automation.Plugins.Shared;
using Automation.Shared.Data;

namespace Automation.Worker.Control.Flow
{
    public class StartTask : ITaskControl
    {
        public static readonly AutomationControl AutomationTask = 
            new AutomationControl(typeof(StartTask))
            {
                Id = Guid.Parse("00000000-0000-0000-0000-100000000002"),
                Metadata = new ScopedMetadata(EnumScopedType.Task) { Name = "Start", Icon = "\uE3D2", IsReadOnly = true },
                Inputs = []
            };

        public Task<TaskResult> DoAsync(object parameters, IProgress<TaskInstanceNotification>? progress)
        { return Task.FromResult(new TaskResult(EnumTaskState.Completed)); }
    }
}
