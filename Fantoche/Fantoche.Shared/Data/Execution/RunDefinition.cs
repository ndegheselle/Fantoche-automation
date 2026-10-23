using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Fantoche.Shared.Data.Graph;
using Fantoche.Shared.Data.Scoped;

namespace Fantoche.Shared.Data.Execution;

/// <summary>
/// What a run depends on, to tell whether it can be resumed : a run walked again on a graph that
/// changed since it started would find nodes that are gone, or skip the ones that were added.
/// <para>
/// Only what the walk reads counts : the nodes, what their inputs are mapped to, how they are
/// connected, and the task each of them runs, nested workflows included. Moving a node or renaming
/// the workflow doesn't keep a run from being resumed.
/// </para>
/// </summary>
public static class RunDefinition
{
    /// <summary>The hash of what a run of [task] depends on, its graph having to be loaded.</summary>
    public static string HashOf(BaseAutomationTask task)
    {
        StringBuilder description = new();
        Describe(task, description, []);
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(description.ToString()));
        return Convert.ToHexString(hash);
    }

    private static void Describe(BaseAutomationTask task, StringBuilder description, HashSet<Guid> described)
    {
        if (!described.Add(task.Id))
            return;

        if (task is AutomationTask automationTask)
        {
            string target = JsonSerializer.Serialize(automationTask.Target);
            description.AppendLine($"task {task.Id} {target} {task.Settings.IsPassingThrough}");
            return;
        }

        if (task is not AutomationWorkflow workflow)
            return;

        description.AppendLine($"workflow {workflow.Id}");
        List<BaseGraphTask> nodes = [.. workflow.Graph.Nodes.OfType<BaseGraphTask>().OrderBy(x => x.Id)];
        foreach (BaseGraphTask node in nodes)
        {
            description.AppendLine($"node {node.Id} {node.TaskId} {node.InputTemplateJson}");
            foreach (GraphConnector connector in node.Inputs.Concat(node.Outputs).OrderBy(x => x.Id))
                description.AppendLine($"connector {connector.Id} {connector.Name}");
        }

        IEnumerable<GraphConnection> connections = workflow.Graph.Connections
            .OrderBy(x => x.SourceId)
            .ThenBy(x => x.TargetId);
        foreach (GraphConnection connection in connections)
            description.AppendLine($"connection {connection.SourceId} {connection.TargetId}");

        // The controls are known by every graph on its own, they never change.
        IEnumerable<BaseAutomationTask> tasks = nodes
            .Where(x => x is not GraphControl)
            .Select(x => x.AutomationTask ?? throw new InvalidOperationException($"The tasks of the workflow '{workflow.Metadata.Name}' are not loaded (is the graph refreshed?)."));
        foreach (BaseAutomationTask nodeTask in tasks)
            Describe(nodeTask, description, described);
    }
}
