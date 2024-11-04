namespace Automation.Plugins.Shared
{
    public enum TaskProgressType
    {
        Info,
        Warning,
        Error,
        Sucess
    }

    public class TaskProgress
    {
        public string Message { get; set; } = string.Empty;
        public TaskProgressType Type { get; set; }
    }

    // Task progress for infos
    // Task lifecycle

    public interface IProgress
    {
        public void Progress(TaskProgress progress);
    }

    public class TaskContext
    {
        public List<Dictionary<string, object>> Scopes { get; set; } = [];
        public Dictionary<string, object>? Shared { get; set; } = null;
        public Dictionary<string, object>? Parameters { get; set; } = null;
    }

    public interface ITask
    {
        public IProgress? Progress { get; set; }
        public Task<Dictionary<string, object>?> ExecuteAsync(TaskContext context);
    }
}
