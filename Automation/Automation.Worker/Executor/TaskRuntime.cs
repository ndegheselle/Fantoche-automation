using Automation.Plugins.Shared;
using Automation.Shared.Data.Graph;

namespace Automation.Worker.Executor;

/// <summary>
/// Engine-side <see cref="ITaskRuntime"/> implementation: holds the running graph node,
/// accumulates the names of activated output branches, and (on demand) translates them
/// into the connector GUIDs the workflow executor uses to drive downstream propagation.
/// </summary>
internal sealed class TaskRuntime : ITaskRuntime
{
    private readonly BaseGraphTask? _node;
    private HashSet<string>? _activatedBranches;

    public TaskRuntime(BaseGraphTask? node, IProgress<TaskNotification>? progress)
    {
        _node = node;
        Progress = progress;
    }

    public IProgress<TaskNotification>? Progress { get; }

    public IReadOnlyList<string> OutputBranches
        => _node?.AutomationTask?.OutputBranches ?? [];

    public void ActivateOutputs(params string[] names)
    {
        if (names == null || names.Length == 0)
            return;

        _activatedBranches ??= new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in names)
            _activatedBranches.Add(name);
    }

    /// <summary>
    /// Resolve the activated branch names to the matching output connector GUIDs on the
    /// current node. Returns <c>null</c> when no branches were activated — interpreted
    /// by the workflow executor as "follow every output."
    /// </summary>
    public HashSet<Guid>? ResolveActivatedConnectorIds()
    {
        if (_activatedBranches == null || _node == null)
            return null;

        var ids = new HashSet<Guid>();
        foreach (var connector in _node.Outputs)
            if (_activatedBranches.Contains(connector.Name))
                ids.Add(connector.Id);

        return ids;
    }
}
