using Automation.Shared.Data.Graph;
using Newtonsoft.Json.Linq;

namespace Automation.Shared.Data.Execution;

/// <summary>
/// Allow an editor to simulate the execution of a workflow to get all the potential errors and resolved references.
/// </summary>
public class GraphExecutionPreview
{
    public Dictionary<Guid, List<string>> NodesErrors { get; } = [];

    public void BuildSamples(TasksGraph graph, GraphContextResolution resolution)
    {
        if (graph.IsRefreshed == false)
            throw new Exception("The graph need to be refreshed before a preview.");

        foreach (var start in graph.GetStartNodes())
        {
            // Merge schema sample with default values
            var startSample = GraphContextResolution.MergeContexts(start.OutputSchema?.ToSampleJson(), start.InputTemplate);
            var startInstance = resolution.CreateInstance(start, startSample, EnumTaskState.Completed);

            GraphExecutionContext context = new()
            {
                Resolution = resolution,
                Graph = graph,
                Node = start,
                Instance = startInstance,
            };

            foreach (var nextNode in graph.GetNext(start))
                RunBranch(context with { Node = nextNode.Task }, new JObject());
        }
    }

    /// <summary>
    /// Walk the branch [context] stands at, previewing every node it leads to.
    /// </summary>
    /// <remarks>
    /// XXX : the shared context is threaded along the walk rather than carried by the instances the
    /// way a run carries it, so it is reset on every node and a share never feeds what comes after
    /// it (see <c>WorkflowExecutor.ResolveControl</c>).
    /// </remarks>
    private void RunBranch(GraphExecutionContext context, JToken? sharedContext)
    {
        // A control drives the walk, everything else stands for the task it runs.
        TaskInstance? instance = context.Node is GraphControl control
            ? PreviewControl(context, control, sharedContext)
            : PreviewTask(context, sharedContext);

        if (instance == null)
            return;

        instance.Shared = sharedContext;
        foreach (var nextNode in context.Graph.GetNext(context.Node))
            RunBranch(context with { Node = nextNode.Task, Instance = instance }, new JObject());
    }

    private TaskInstance? PreviewControl(GraphExecutionContext context, GraphControl control, JToken? sharedContext)
    {
        if (control.IsJoin())
        {
            var waitingInstance = context.Resolution.GetOrCreateWaitingInstance(control, context.Instance);

            var previousNodes = context.Graph.GetPrevious(control);
            if (context.Resolution.TryGetAllInstances(previousNodes, out var instances))
            {
                var result = context.Resolution.GetInputFor(control, instances, sharedContext);
                if (result.HasError)
                {
                    NodesErrors.Add(control.Id, result.Errors);
                    return null;
                }

                waitingInstance.Previous = context.Instance;
                waitingInstance.State = EnumTaskState.Completed;
                waitingInstance.Shared = sharedContext;
                // XXX : which mean that the outpute schema have to be updated then setting up
                waitingInstance.Output = control.OutputSchema?.ToSampleJson();
                return waitingInstance;
            }
        }
        else if (control.IsShare())
        {
            // We pass on each node exactly once
            if (context.Resolution.NodeInstances.ContainsKey(control.Id))
                return null;

            // XXX : what the share resolved is dropped rather than merged into the shared context,
            // see RunBranch.
            var result = context.Resolution.GetInputFor(control, context.Instance, sharedContext);
            if (result.HasError)
            {
                NodesErrors.Add(control.Id, result.Errors);
                return null;
            }

            return context.Resolution.CreateInstance(control, result.Token, EnumTaskState.Completed, context.Instance);
        }

        return null;
    }

    private TaskInstance? PreviewTask(GraphExecutionContext context, JToken? sharedContext)
    {
        // We pass on each node exactly once
        if (context.Resolution.NodeInstances.ContainsKey(context.Node.Id))
            return null;

        // We don't really need the input here, we just check if there is any errors in mapping
        var result = context.Resolution.GetInputFor(context.Node, context.Instance, sharedContext);
        if (result.HasError)
        {
            NodesErrors.Add(context.Node.Id, result.Errors);
            return null;
        }

        var instance = context.Resolution.CreateInstance(context.Node, result.Token, EnumTaskState.Completed, context.Instance);
        instance.Output = context.Node.OutputSchema?.ToSampleJson();

        return instance;
    }
}
