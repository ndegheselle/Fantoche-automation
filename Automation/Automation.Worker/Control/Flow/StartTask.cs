using Automation.Dal.Models;
using Automation.Plugins.Shared;
using Automation.Shared.Data;

namespace Automation.Worker.Control.Flow
{
    public class StartTask : ITaskControl
    {
        public readonly static AutomationTask Task = new AutomationTask()
        {
            ParentId = controlsScopeId,
            ParentTree = [Scope.ROOT_SCOPE_ID, controlsScopeId],
            Metadata = new ScopedMetadata(EnumScopedType.Task)
            {
                Name = "Start",
                Icon = "\uf04b",
                IsReadOnly = true
            },
            Target = new ClassTarget(new ClassIdentifier()
            {
                Dll = "internal.controls",
                Name = typeof(StartTask).Name,
            })
        };

        public Task<EnumTaskState> DoAsync(TaskParameters parameters, IProgress<TaskInstanceNotification>? progress)
        {
            return Task.FromResult(EnumTaskState.Completed);
        }
    }
}
