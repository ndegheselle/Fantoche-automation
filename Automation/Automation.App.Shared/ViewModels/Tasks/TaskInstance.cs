using Automation.Plugins.Shared;
using Automation.Shared.Data;

namespace Automation.App.Shared.ViewModels.Tasks
{
    public class TaskInstance : ITaskInstance
    {
        public Guid Id { get; set; }

        public Guid TaskId { get; set; }
        public TargetedPackage Target { get; set; }
        public Guid ScopeId { get; set; }
        public string? WorkerId { get; set; }

        public TaskContext Context { get; set; }
        public dynamic? Result { get; set; }

        public EnumTaskState State { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
