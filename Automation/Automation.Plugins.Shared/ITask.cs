namespace Automation.Plugins.Shared
{
    public class TaskProgress
    {
        public string Message { get; set; } = string.Empty;
    }

    public interface IProgress
    {
        public void Progress(TaskProgress progress);
    }

    public class TaskContext
    {
        public List<dynamic> Scopes { get; set; } = [];
        public dynamic? Shared { get; set; }
        public dynamic? Parameters { get; set; }
    }

    public interface ITask
    {
        public IProgress? Progress { get; set; }
        public Task<dynamic?> ExecuteAsync(TaskContext context);
    }
}
