using Automation.Plugins.Shared;

namespace Automation.Worker.Control.Flow
{
    internal class StartTask : ITask
    {
        public Task<EnumTaskState> DoAsync(TaskParameters parameters, IProgress<TaskProgress>? progress)
        {
            return Task.FromResult(EnumTaskState.Completed);
        }
    }
}
