using Automation.Plugins.Shared;
using Automation.Shared.Data.Graph;

namespace Automation.Plugins.Control;

public class ControlContext
{
    public object? Parameters { get; set; }
    public BaseGraphTask CurrentNode { get; set; }
    public ControlContext(BaseGraphTask currentNode, object? parameters = null)
    {
        Parameters = parameters;
        CurrentNode = currentNode;
    }
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
    public Task<ControlOutput> DoAsync(ControlContext input, IProgress<TaskNotification>? progress = null, CancellationToken? cancellation = null);
}

public abstract class BaseControlTask : ITaskControl
{
    public TaskConnector? Input { get; } = new() { Type = typeof(ControlContext) };
    public TaskConnector? Output { get; } = new() { Type = typeof(ControlOutput) };

    public async Task<object?> DoAsync(object? parameters, IProgress<TaskNotification>? progress = null,
        CancellationToken? cancellation = null)
    {
        if (parameters is not ControlContext context)
            throw new ArgumentException($"Parameters are not of expected type '{Input!.Type}'.", nameof(parameters));

        return await DoAsync(context, progress, cancellation);
    }

    public abstract Task<ControlOutput> DoAsync(ControlContext context, IProgress<TaskNotification>? progress = null, CancellationToken? cancellation = null);
}