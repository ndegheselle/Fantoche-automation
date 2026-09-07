using Automation.Shared.Data.Graph;
using Newtonsoft.Json.Linq;

namespace Automation.Shared.Data.Execution;

public class GraphExecutionPreview
{
    public Dictionary<Guid, List<string>> NodesErrors { get; } = [];

    // XXX : graph need to be refreshed
    public void BuildSamples(TasksGraph graph, GraphContextResolution resolution)
    {
        foreach (var start in graph.GetStartNodes())
        {
            // Merge schema sample with default values
            var startSample = GraphContextResolution.MergeContexts(start.OutputSchema?.ToSampleJson(), start.InputTemplate);
            var instance = resolution.CreateInstance(start, startSample, EnumTaskState.Completed);

            foreach (var nextNode in graph.GetNext(start))
                RunBranch(graph, nextNode.Task, instance, new JObject(), resolution);
        }
    }

    public void RunBranch(TasksGraph graph, BaseGraphTask node, TaskInstance? previous, JToken? sharedContext, GraphContextResolution resolution)
    {
        // Control task
        TaskInstance? instance = null;
        if (node is GraphControl control)
            instance = PreviewControl(graph, control, previous, sharedContext, resolution);
        // All other tasks
        else
            instance = PreviewTask(graph, node, previous, sharedContext, resolution);

        if (instance == null)
            return;

        instance.Shared = sharedContext;
        foreach (var nextNode in graph.GetNext(node))
            RunBranch(graph, nextNode.Task, instance, new JObject(), resolution);
    }

    private TaskInstance? PreviewControl(TasksGraph graph, GraphControl control, TaskInstance? previous, JToken? sharedContext, GraphContextResolution resolution)
    {
        if (control.IsJoin())
        {
            var waitingInstance = resolution.GetOrCreateWaitingInstance(control, previous);

            var previousNodes = graph.GetPrevious(control);
            if (resolution.TryGetAllInstances(previousNodes, out var instances))
            {
                var result = resolution.GetInputFor(control, instances, sharedContext);
                if (result.HasError)
                {
                    NodesErrors.Add(control.Id, result.Errors);
                    return null;
                }

                waitingInstance.Previous = previous;
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
            if (resolution.NodeInstances.ContainsKey(control.Id))
                return null;

            var result = resolution.GetInputFor(control, previous == null ? [] : [previous], sharedContext);
            if (result.HasError)
            {
                NodesErrors.Add(control.Id, result.Errors);
                return null;
            }
            sharedContext = GraphContextResolution.MergeContexts(sharedContext, result.Token);
            return resolution.CreateInstance(control, result.Token, EnumTaskState.Completed, previous);
        }

        return null;
    }

    private TaskInstance? PreviewTask(TasksGraph graph, BaseGraphTask node, TaskInstance? previous, JToken? sharedContext, GraphContextResolution resolution)
    {
        // We pass on each node exactly once
        if (resolution.NodeInstances.ContainsKey(node.Id))
            return null;

        // We don't really need the input here, we just check if there is any errors in mapping
        var result = resolution.GetInputFor(node, previous, sharedContext);
        if (result.HasError)
        {
            NodesErrors.Add(node.Id, result.Errors);
            return null;
        }

        var instance = resolution.CreateInstance(node, result.Token, EnumTaskState.Completed, previous);
        instance.Output = node.OutputSchema?.ToSampleJson();

        return instance;
    }
}