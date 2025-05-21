using Automation.Plugins.Shared;

namespace Automation.Shared.Data
{
    public interface ITaskInstance : IIdentifier
    {
        public Guid TaskId { get; set; }
        public Guid? NodeId { get; set; } 

        public string? WorkerId { get; set; }

        public string SettingsJson { get; set; }
        public string ContextJson { get; set; }

        public EnumTaskState State { get; set; }

        public DateTime CreateDate { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
