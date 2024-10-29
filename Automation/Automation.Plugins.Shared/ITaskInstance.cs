namespace Automation.Plugins.Shared
{
    public enum EnumTaskState
    {
        Pending,
        Progress,
        Completed,
        Failed
    }

    public class TaskProgress
    {
        public EnumTaskState State { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public interface IExecutor
    {
        public void Progress(TaskProgress progress);
    }
}
