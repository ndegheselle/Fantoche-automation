using Automation.Plugins.Shared;

namespace Automation.Shared.Data.Task
{
    /// <summary>
    /// Notification of a task instance, used to report the progress of a task execution.
    /// </summary>
    public class TaskInstanceNotification
    {
        public Guid? WorkflowInstanceId { get; set; }
        public Guid InstanceId { get; set; }
        public TaskNotification Notification { get; set; }

        public TaskInstanceNotification(Guid instanceId, TaskNotification data)
        {
            InstanceId = instanceId;
            Notification = data;
        }
    }

    /// <summary>
    /// State of a task instance, used to track the lifecycle of a task.
    /// </summary>
    public class TaskInstanceState
    {
        public Guid? WorkflowInstanceId { get; set; }
        public Guid InstanceId { get; set; }

        public EnumTaskState State { get; set; }

        public TaskInstanceState(Guid instanceId, EnumTaskState state)
        {
            InstanceId = instanceId;
            State = state;
        }
    }
}