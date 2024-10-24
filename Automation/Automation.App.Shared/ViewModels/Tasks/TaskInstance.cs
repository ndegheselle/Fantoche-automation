using Automation.Shared.Data;

namespace Automation.App.Shared.ViewModels.Tasks
{
    public class TaskInstance : ITaskInstance
    {
        public Guid Id { get; set; }
        public Guid? ParentTaskId { get; set; }
        public Guid ScopeId { get; set; }
        public Guid TaskId { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public string WorkerId { get; set; }
        public object? Parameters { get; set; }
        public EnumTaskState State { get; set; }
    }
}
