using Automation.Shared.Data.Graph;

namespace Automation.Shared.Data.Execution;

/// <summary>
/// Where a walk of a graph stands : the node it reached, the instance it carries on from and what
/// every node already walked resolved to. Copied rather than mutated as the walk moves on (see the
/// <c>with</c> expression), so a branch never sees where another one went — what they do share is
/// held by <see cref="GraphContextResolution"/>.
/// <para>
/// Common to anything walking a graph : a run (see <c>WorkflowExecutor</c>) and the preview an
/// editor shows of it (see <see cref="GraphExecutionPreview"/>) only differ by what they do on each
/// node, not by what they need to know to get there.
/// </para>
/// </summary>
public record GraphExecutionContext
{
    /// <summary>
    /// The instances of the walk, and what the mappings of the nodes are resolved against.
    /// </summary>
    public required GraphContextResolution Resolution { get; init; }

    /// <summary>
    /// The graph being walked. Expected to be refreshed : the walk reads the nodes a connection
    /// leads to rather than the ids it holds.
    /// </summary>
    public required TasksGraph Graph { get; init; }

    /// <summary>
    /// The node the walk reached.
    /// </summary>
    public required BaseGraphTask Node { get; init; }

    /// <summary>
    /// The instance <see cref="Node"/> carries on from : the one of the node before it until it ran,
    /// its own once it did.
    /// </summary>
    public required TaskInstance Instance { get; init; }
}
