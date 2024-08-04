using Automation.Shared.Data;

namespace Automation.Dal.Models
{
    public class TaskHistory : ITaskHistory
    {
        public Guid Id { get; set; }
        public Guid? ParentTaskId { get; set; }
        public Guid ScopeId { get; set; }
        public Guid TaskId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public EnumInstanceStatus Status { get; set; }
    }
}
