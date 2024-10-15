namespace Automation.Shared.Data
{

    public enum EnumInstanceStatus
    {
        Pending,
        Running,
        Stopped,
        Completed,
        Failed
    }

    public interface ITaskHistory : IIdentifier
    {
        // XXX : come from the Task, there to simplify request, what about updates (change task scope)
        Guid ScopeId { get; set; }
        Guid? ParentTaskId { get; set; }
        Guid TaskId { get; set; }
        DateTime StartDate { get; set; }
        DateTime? EndDate { get; set; }
        EnumInstanceStatus Status { get; set; }
    }

    public interface IScheduledTask : IIdentifier
    {
        Guid TaskId { get; set; }
        int? Year { get; set; }
        int? Month { get; set; }
        int? Day { get; set; }
        int? Hour { get; set; }
        int? Minute { get; set; }
        int? DayOfWeek { get; set; }
    }
}
