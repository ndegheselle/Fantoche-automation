using Automation.Plugins.Shared;

namespace Automation.Realtime.Models
{
    /// <summary>
    /// Notification of a task instance, used to report the progress of a task execution.
    /// </summary>
    public class TaskInstanceNotification
    {
        public Guid InstanceId { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.Now;
        public EnumTaskProgress Type { get; set; }
    }

    /// <summary>
    /// State of a task instance, used to track the lifecycle of a task.
    /// </summary>
    public class TaskIntanceState
    {
        public Guid InstanceId { get; set; }
        public EnumTaskState State { get; set; }

        public TaskIntanceState(Guid instanceId, EnumTaskState state)
        {
            InstanceId = instanceId;
            State = state;
        }
    }
}