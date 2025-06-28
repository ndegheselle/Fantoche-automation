namespace Automation.Plugins.Shared
{
    public enum EnumTaskProgress
    {
        Info,
        Warning,
        Error,
        Sucess
    }

    [Flags]
    public enum EnumTaskState
    {
        /// <summary>
        /// Waiting for a worker to execute the task
        /// </summary>
        Pending = 1,
        /// <summary>
        /// Task is waiting on another task, a manual action or a timer
        /// </summary>
        Waiting = 2,
        /// <summary>
        /// Task is progressing
        /// </summary>
        Progressing = 4,
        /// <summary>
        /// Task is completed
        /// </summary>
        Completed = 8,
        /// <summary>
        /// Task failed
        /// </summary>
        Failed = 16,
        /// <summary>
        /// Task is canceled
        /// </summary>
        Canceled = 32,
        /// <summary>
        /// Task is finished
        /// </summary>
        Finished = Completed | Failed | Canceled
    }

    /// <summary>
    /// Notification of a task instance, used to report the progress of a task execution.
    /// </summary>
    public class TaskInstanceNotification
    {
        public Guid InstanceId { get; set; }
        public string Message { get; set; } = string.Empty;
        public EnumTaskProgress Type { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
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

    public class TaskParameters
    {
        public string SettingsJson { get; private set; }
        public string ContextJson { get; set; }

        public TaskParameters(string settingsJson, string contextJson)
        {
            SettingsJson = settingsJson;
            ContextJson = contextJson;
        }
    }

    public interface ITask
    {
        /// <summary>
        /// Execute the task asynchronously and return the resultin state of the task.
        /// </summary>
        /// <param name="parameters">Context of execution of the task, the context can be modified by the task to pass data to next tasks</param>
        /// <param name="progress"></param>
        /// <returns></returns>
        public Task<EnumTaskState> DoAsync(TaskParameters parameters, IProgress<TaskInstanceNotification>? progress);
    }
}
