using Automation.Plugins.Shared;

namespace Automation.Shared.Data.Task
{
    /// <summary>
    /// Notification of a task instance, used to report the progress of a task execution.
    /// </summary>
    public class TaskInstanceNotification
    {
        public Guid InstanceId { get; set; }
        public TaskNotification Data { get; set; }
    }

    /// <summary>
    /// State of a task instance, used to track the lifecycle of a task.
    /// </summary>
    public class TaskIntanceState
    {
        public Guid InstanceId { get; set; }
        public EnumTaskState State { get; set; }
    }
}