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

    public class TaskProgress
    {
        public string Message { get; set; } = string.Empty;
        public EnumTaskProgress Type { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
    }

    public class TaskContext
    {
        public string SettingsJson { get; private set; }
        public string ContextJson { get; set; }

        public TaskContext(string settingsJson, string contextJson)
        {
            SettingsJson = settingsJson;
            ContextJson = contextJson;
        }
    }

    public interface ITask
    {
        public Task<EnumTaskState> DoAsync(TaskContext context, IProgress<TaskProgress>? progress);
    }
}
