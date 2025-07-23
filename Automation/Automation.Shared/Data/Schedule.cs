namespace Automation.Shared.Data
{
    public enum EnumTaskConnectorType
    {
        Data,
        Flow
    }

    public enum EnumTaskConnectorDirection
    {
        In,
        Out
    }

    public class TaskSchedule
    {
        public Guid TaskId { get; set; }
        public Schedule Schedule { get; set; }

        public TaskSchedule(Guid taskId, Schedule schedule)
        {
            TaskId = taskId;
            Schedule = schedule;
        }
    }

    public class Schedule
    {
        public string CronExpression { get; set; } = "";
        public string JsonSettings { get; set; } = "";
    }
}
