using Automation.Plugins.Shared;

namespace Automation.Shared.Data
{
    public interface ITaskInstance : IIdentifier
    {
        public Guid TaskId { get; set; }
        public string? WorkerId { get; set; }
        public TaskParameters Parameters { get; set; }

        public EnumTaskState State { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
    }
}
