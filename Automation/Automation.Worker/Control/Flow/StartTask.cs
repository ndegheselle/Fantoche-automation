using Automation.Dal.Models;
using Automation.Plugins.Shared;
using Automation.Shared.Data;

namespace Automation.Worker.Control.Flow
{
    public class StartTask : ITaskControl
    {
        /// <summary>
        /// Register the task as an available task
        /// </summary>
        static StartTask()
        {
            ControlsTasks.Register<StartTask>(
                new AutomationControl(typeof(StartTask))
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-100000000002"),
                    Metadata =
                        new ScopedMetadata(EnumScopedType.Task) { Name = "Start", Icon = "\uf04b", IsReadOnly = true }
                });
        }

        public Task<EnumTaskState> DoAsync(TaskParameters parameters, IProgress<TaskInstanceNotification>? progress)
        {
            return Task.FromResult(EnumTaskState.Completed);
        }
    }
}
