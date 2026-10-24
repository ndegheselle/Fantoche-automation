using System.Text.Json;
using Fantoche.Services.Local.Models;
using Fantoche.Shared.Data.Graph;

namespace Fantoche.Services.Local.Database;

/// <summary>A node of a stored graph with its connectors : what a version keeps of a node it changed.</summary>
internal sealed record StoredNode(GraphNodeModel Node, List<GraphConnectorModel> Connectors);

/// <summary>
/// A graph as the store holds it : its rows, which is what two versions are compared on and what
/// a past version is rebuilt into.
/// </summary>
internal sealed class GraphSnapshot
{
    public Dictionary<Guid, StoredNode> Nodes { get; } = [];
    public HashSet<GraphConnectionModel> Connections { get; } = [];

    /// <summary>
    /// The rows of the graph of [workflowId]. A connection whose connectors are gone is left out :
    /// the graph couldn't be refreshed with it anyway, its connectors being resolved by id.
    /// </summary>
    public static GraphSnapshot From(TasksGraph graph, Guid workflowId)
    {
        GraphSnapshot snapshot = new();
        HashSet<Guid> connectorIds = [];
        foreach (GraphNode node in graph.Nodes)
        {
            List<GraphConnectorModel> connectors = [];
            if (node is BaseGraphTask task)
            {
                connectors.AddRange(task.Inputs.Select((x, position) => GraphConnectorModel.From(x, task.Id, true, position)));
                connectors.AddRange(task.Outputs.Select((x, position) => GraphConnectorModel.From(x, task.Id, false, position)));
            }

            connectorIds.UnionWith(connectors.Select(x => x.Id));
            snapshot.Nodes[node.Id] = new StoredNode(GraphNodeModel.From(node, workflowId), connectors);
        }

        IEnumerable<GraphConnectionModel> connections = graph.Connections
            .Where(x => connectorIds.Contains(x.SourceId) && connectorIds.Contains(x.TargetId))
            .Select(GraphConnectionModel.From);
        snapshot.Connections.UnionWith(connections);
        return snapshot;
    }

    /// <summary>The graph the rows stand for, to be refreshed with its tasks before it is walked.</summary>
    public TasksGraph ToGraph()
    {
        TasksGraph graph = new();
        foreach (StoredNode stored in Nodes.Values)
        {
            GraphNode node = stored.Node.ToNode();
            graph.Nodes.Add(node);

            // Only the nodes running something hold connectors, a group is drawn and nothing else.
            if (node is not BaseGraphTask task)
                continue;

            // In the order they are drawn in : an output branch is known by its place in the list.
            foreach (GraphConnectorModel connector in stored.Connectors.OrderBy(x => x.Position))
                (connector.IsInput ? task.Inputs : task.Outputs).Add(connector.ToConnector());
        }

        foreach (GraphConnectionModel connection in Connections)
            graph.Connections.Add(connection.ToConnection());
        return graph;
    }
}

/// <summary>
/// What turns a version of a graph into another one : the nodes that differ, stored whole with
/// their connectors, and the connections added and removed. A past version only keeps these, which
/// is what keeps the versions of a workflow light.
/// </summary>
internal sealed class GraphChanges
{
    /// <summary>The nodes to set, null for a node to remove.</summary>
    public Dictionary<Guid, StoredNode?> Nodes { get; init; } = [];
    public List<GraphConnectionModel> AddedConnections { get; init; } = [];
    public List<GraphConnectionModel> RemovedConnections { get; init; } = [];

    public bool IsEmpty => Nodes.Count == 0 && AddedConnections.Count == 0 && RemovedConnections.Count == 0;

    /// <summary>What turns [from] into [to].</summary>
    public static GraphChanges Between(GraphSnapshot from, GraphSnapshot to)
    {
        GraphChanges changes = new();
        foreach ((Guid id, StoredNode node) in to.Nodes)
        {
            // Compared as written : a record holding a list only compares the list by reference.
            bool isSame = from.Nodes.TryGetValue(id, out StoredNode? before) && Json(before) == Json(node);
            if (!isSame)
                changes.Nodes[id] = node;
        }

        foreach (Guid id in from.Nodes.Keys.Where(x => !to.Nodes.ContainsKey(x)))
            changes.Nodes[id] = null;

        changes.AddedConnections.AddRange(to.Connections.Except(from.Connections));
        changes.RemovedConnections.AddRange(from.Connections.Except(to.Connections));
        return changes;
    }

    public void ApplyTo(GraphSnapshot snapshot)
    {
        foreach ((Guid id, StoredNode? node) in Nodes)
        {
            if (node == null)
                snapshot.Nodes.Remove(id);
            else
                snapshot.Nodes[id] = node;
        }

        snapshot.Connections.ExceptWith(RemovedConnections);
        snapshot.Connections.UnionWith(AddedConnections);
    }

    public string ToJson() => JsonSerializer.Serialize(this);

    public static GraphChanges FromJson(string json)
        => JsonSerializer.Deserialize<GraphChanges>(json) ?? throw new InvalidDataException("A version of a graph holds no changes.");

    private static string Json(StoredNode node) => JsonSerializer.Serialize(node);
}
