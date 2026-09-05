using Automation.Shared.Data.Graph;

namespace Automation.Shared.Data.Execution;

public class GraphContextResolutionException : Exception
{
    public GraphContextResolutionException(string message) : base(message)
    { }
}

/// <summary>
/// What the nodes of a running workflow read : the same <see cref="GraphContext"/> an editor builds
/// from samples, filled with what the branches actually produced. Only the values differ, the shape
/// and the resolution of a mapping are shared.
/// </summary>
public class GraphExecutionContext
{
    private readonly WorkflowInstance _workflowInstance;

    public GraphExecutionContext(WorkflowInstance workflowInstance)
    {
        _workflowInstance = workflowInstance;
    }

    /// <summary>
    /// The context of a node reached by [previousInstance] : what that branch produced, along with
    /// the shared values the run has gathered so far.
    /// </summary>
    public GraphContext GetInstanceContextFor(TaskInstance previousInstance)
    {
        TaskInstance previous = ResolveEffectiveInstance(previousInstance);
        return GraphContext.From(
            previous.NodeName,
            previous.Output,
            _workflowInstance.SharedContext,
            _workflowInstance.GlobalContext);
    }

    /// <summary>
    /// The single context of a node that waited for every branch reaching it, what each produced
    /// being held under the name of the node it comes from.
    /// </summary>
    public GraphContext GetWaitedInstanceContextFor(IReadOnlyList<TaskInstance> instances)
    {
        var previouses = instances.Select(ResolveEffectiveInstance).ToDictionary(x => x.NodeName, x => x.Output);
        return GraphContext.From(previouses, _workflowInstance.SharedContext, _workflowInstance.GlobalContext);
    }

    /// <summary>
    /// What an instance actually stands for : an instance of a node passing through produces
    /// nothing of its own, what it was reached with is what the next nodes read.
    /// </summary>
    private TaskInstance ResolveEffectiveInstance(TaskInstance instance)
    {
        if (instance.Node?.AutomationTask?.Settings.IsPassingThrough != true)
            return instance;
        if (instance.Previous == null)
            throw new GraphContextResolutionException("Could not resolve an effective task instance.");
        return ResolveEffectiveInstance(instance.Previous);
    }
}
