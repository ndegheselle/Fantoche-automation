using Automation.Shared.Data.Graph;

namespace Automation.Shared.Data.Execution;

/// <summary>
/// What a branch of the graph can still bring to the node reading it.
/// </summary>
public enum EnumBranchLiveness
{
    /// <summary>
    /// Something is still running upstream : the branch may yet deliver an output.
    /// </summary>
    Pending,
    /// <summary>
    /// The node ran and handed an output over, which flowed (or is flowing) downstream.
    /// </summary>
    Delivered,
    /// <summary>
    /// Nothing will ever come out of the branch : every path feeding it ended without an output.
    /// </summary>
    Dead,
}

/// <summary>
/// How a node waiting on every branch reaching it (a join, an end) stands.
/// </summary>
public enum EnumWaitResolution
{
    /// <summary>
    /// At least one branch can still deliver : the node keeps waiting.
    /// </summary>
    Pending,
    /// <summary>
    /// Every branch that could deliver did : the node can run with what they gave.
    /// </summary>
    Resolved,
    /// <summary>
    /// No branch delivered anything and none can anymore : the node will never run. A node is
    /// normally never reached at all in that case (the branches dying before they get to it),
    /// this is what keeps it from waiting for good if it is.
    /// </summary>
    Dead,
}

/// <summary>
/// Liveness of the branches of a running graph, crawled from the instances the run produced.
/// <para>
/// A branch dies whenever a node ends without handing an output over : its output was deactivated
/// (a conditional closing it), it failed or it was canceled. A node waiting on all of its branches
/// has to tell such a branch from one still working, otherwise a single conditional blocks the
/// whole graph : hence the crawl, a branch being dead when every path leading to it is.
/// </para>
/// </summary>
public static class BranchLiveness
{
    /// <summary>
    /// Liveness of the branch ending on [node], deduced from the instances of the run and,
    /// when the node itself hasn't run, from the nodes feeding it.
    /// </summary>
    /// <param name="graph">Graph being run, refreshed.</param>
    /// <param name="instancesOf">Instances a node of the graph produced during the run.</param>
    /// <param name="node">Last node of the branch.</param>
    public static EnumBranchLiveness Resolve(
        TasksGraph graph,
        Func<BaseGraphTask, IReadOnlyList<TaskInstance>> instancesOf,
        BaseGraphTask node)
    {
        return Resolve(graph, instancesOf, node, []);
    }

    /// <summary>
    /// Whether [instance] handed an output over to the nodes after it. A completed instance
    /// without any output had it deactivated : the branch died there.
    /// </summary>
    public static bool HasDelivered(TaskInstance instance)
    {
        return instance.State == EnumTaskState.Completed && instance.Output != null;
    }

    private static EnumBranchLiveness Resolve(
        TasksGraph graph,
        Func<BaseGraphTask, IReadOnlyList<TaskInstance>> instancesOf,
        BaseGraphTask node,
        HashSet<Guid> crawled)
    {
        // A cycle carries no life of its own : it is entered from the outside, and those edges
        // are crawled on their own.
        if (!crawled.Add(node.Id))
            return EnumBranchLiveness.Dead;

        IReadOnlyList<TaskInstance> instances = instancesOf(node);
        if (instances.Count > 0)
        {
            // Ran and gave something to the nodes after it, on this turn or on a previous one.
            if (instances.Any(HasDelivered))
                return EnumBranchLiveness.Delivered;

            // Still running : its outcome isn't known yet.
            if (instances.Any(x => (x.State & EnumTaskState.Finished) == 0 && x.State != EnumTaskState.Waiting))
                return EnumBranchLiveness.Pending;

            // Holding on the branches reaching it (a join, an end) : they decide for it.
            if (instances.Any(x => x.State == EnumTaskState.Waiting))
                return ResolvePrevious(graph, instancesOf, node, crawled);

            // Every run of the node ended without an output : deactivated, failed or canceled.
            return EnumBranchLiveness.Dead;
        }

        // Never reached : it only runs if one of the nodes feeding it still can. A node without
        // any input connection is a start of the graph, whose instance exists before anything
        // else runs, so not having one here means it will never run.
        return ResolvePrevious(graph, instancesOf, node, crawled);
    }

    /// <summary>
    /// Liveness a node inherits from the nodes feeding it : dead only when every one of them is.
    /// A branch that already delivered leaves the node pending, it is about to run.
    /// </summary>
    private static EnumBranchLiveness ResolvePrevious(
        TasksGraph graph,
        Func<BaseGraphTask, IReadOnlyList<TaskInstance>> instancesOf,
        BaseGraphTask node,
        HashSet<Guid> crawled)
    {
        foreach (BaseGraphTask previous in graph.GetPrevious(node).DistinctBy(x => x.Id))
        {
            // Each path is crawled with its own trail : a node shared by two of them can be dead
            // through one and alive through the other.
            if (Resolve(graph, instancesOf, previous, new HashSet<Guid>(crawled)) != EnumBranchLiveness.Dead)
                return EnumBranchLiveness.Pending;
        }

        return EnumBranchLiveness.Dead;
    }
}
