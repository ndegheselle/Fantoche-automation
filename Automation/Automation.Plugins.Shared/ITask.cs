namespace Automation.Plugins.Shared
{
    public enum EnumTaskProgress
    {
        Info,
        Warning,
        Error,
        Sucess
    }

    public enum EnumTaskState
    {
        /// <summary>
        /// Waiting for a worker to execute the task
        /// </summary>
        Pending,
        /// <summary>
        /// Task is waiting on another task, a manual action or a timer
        /// </summary>
        Waiting,
        /// <summary>
        /// Task is progressing
        /// </summary>
        Progressing,
        /// <summary>
        /// Task is completed
        /// </summary>
        Completed,
        /// <summary>
        /// Task failed
        /// </summary>
        Failed,
        /// <summary>
        /// Task is canceled
        /// </summary>
        Canceled,
    }

    public class TaskProgress
    {
        public string Message { get; set; } = string.Empty;
        public EnumTaskProgress Type { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
    }

    public class TaskContext
    {
        public string? SettingsJson { get; set; }
    }

    public interface ITask
    {
        public Task<EnumTaskState> DoAsync(TaskContext context, IProgress<TaskProgress>? progress);
    }

    public interface IResultsTask : ITask
    {
        public Dictionary<string, object> Results { get; }
    }
}
