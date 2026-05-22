using Automation.Plugins.Shared;
using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Graph;
using Automation.Shared.Data.Scoped;
using Newtonsoft.Json.Linq;

namespace Automation.Worker.Executor;

public record TaskOutput
{
    public JToken? OutputToken { get; set; }
    public EnumTaskState State { get; set; }

    /// <summary>
    /// When set, only these output connector IDs will be followed. null means all outputs active.
    /// </summary>
    public HashSet<Guid>? ActiveOutputConnectorIds { get; set; }
}

public interface ITaskExecutor
{
    Task<TaskOutput> ExecuteAsync(
        BaseAutomationTask automationTask,
        JToken? input,
        IProgress<TaskNotification>? notifications = null,
        CancellationToken? cancellation = null);
}