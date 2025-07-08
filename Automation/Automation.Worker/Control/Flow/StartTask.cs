using Automation.Plugins.Shared;
using Automation.Shared.Data;

namespace Automation.Worker.Control.Flow
{
    public class StartTask : ITaskControl
    {
        public readonly static Guid Id = Guid.Parse("00000000-0000-0000-0000-000000000010");
        public readonly static ClassIdentifier Identifier = new ClassIdentifier()
        {
            Dll = "internal.controls",
            Name = nameof(StartTask)
        };

        public Task<EnumTaskState> DoAsync(TaskParameters parameters, IProgress<TaskInstanceNotification>? progress)
        {
            return Task.FromResult(EnumTaskState.Completed);
        }
    }
}
