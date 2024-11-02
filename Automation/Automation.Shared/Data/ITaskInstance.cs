using Automation.Plugins.Shared;

namespace Automation.Shared.Data
{
    public enum EnumTaskState
    {
        Pending,
        Progress,
        Completed,
        Failed
    }

    public interface ITaskInstance : IIdentifier
    {
        public Guid TaskId { get; set; }
        public TargetedPackage Target { get; set; }
        public string? WorkerId { get; set; }

        public TaskContext Context { get; set; }
        public dynamic? Result { get; set; }

        public EnumTaskState State { get; set; }

        public DateTime CreateDate { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
