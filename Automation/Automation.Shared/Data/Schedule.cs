namespace Automation.Shared.Data
{
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
