using Automation.Plugins.Shared;

namespace Automation.Shared.Data
{
    public interface IAutomationTaskInstance : IIdentifier
    {
        public Guid TaskId { get; set; }
        public string? WorkerId { get; set; }
        public TaskParameters Parameters { get; set; }

        public EnumTaskState State { get; set; }

        public DateTime CreateDate { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
