namespace Automation.Plugins.Shared
{
    public class ControlInput
    {
        public object? Parameters { get; set; }
        public BaseGraphTask CurrentNode { get; set; }
    }

    public class ControlOutput
    {
        public object? Result { get; set; }
        /// <summary>
        /// When set, only these output connector IDs will be followed. null means all outputs active.
        /// </summary>
        public HashSet<Guid>? ActiveOutputConnectorIds { get; set; }
    }

    public interface ITaskControl : ITask
    {
        public Task<ControlOutput> DoAsync(ControlInput input, IProgress<TaskNotification>? progress = null, CancellationToken? cancellation = null);
    }
}
