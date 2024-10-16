namespace Automation.Shared.Data
{
    public enum EnumTaskState
    {
        Pending,
        Progress,
        Success,
        Error
    }

    public interface ITaskInstance : IIdentifier
    {
        public Guid TaskId { get; set; }
        public string WorkerId { get; set; }

        public dynamic Parameters { get; set; }
        public EnumTaskState State { get; set; }
    }
}
