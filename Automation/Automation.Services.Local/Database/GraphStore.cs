using System.Data;
using Automation.Services.Local.Models;
using Automation.Shared.Data.Graph;
using Automation.Shared.Data.Scoped;
using Dapper;

namespace Automation.Services.Local.Database;

/// <summary>
/// The graph of a workflow, spread over the nodes, their connectors and the connections between
/// them : read and written as a whole, the three tables never making sense on their own.
/// </summary>
internal static class GraphStore
{
    /// <summary>
    /// The graphs of [workflowIds], every workflow given one even when its graph is empty. The
    /// three tables are read once for the whole set rather than once per workflow.
    /// </summary>
    public static async Task<Dictionary<Guid, TasksGraph>> LoadAsync(
        IDbConnection connection,
        IReadOnlyCollection<Guid> workflowIds)
    {
        Dictionary<Guid, TasksGraph> graphs = workflowIds.ToDictionary(x => x, _ => new TasksGraph());
        if (workflowIds.Count == 0)
            return graphs;

        var nodeRows = await connection.QueryAsync<GraphNodeModel>($"""
            SELECT {GraphNodeModel.Columns} FROM GraphNodes WHERE WorkflowId IN @workflowIds;
            """,
            new { workflowIds });

        // A connector is read with the node it hangs under, in the order it is drawn in.
        var connectorRows = await connection.QueryAsync<GraphConnectorModel>($"""
            SELECT {GraphConnectorModel.Columns} FROM GraphConnectors
            WHERE NodeId IN (SELECT Id FROM GraphNodes WHERE WorkflowId IN @workflowIds)
            ORDER BY Position;
            """,
            new { workflowIds });

        // A connection is held by the graph of the node its source belongs to, the two ends of a
        // connection always being in the same graph.
        var connectionRows = await connection.QueryAsync<GraphConnectionModel>($"""
            SELECT {GraphConnectionModel.Columns} FROM GraphConnections
            WHERE SourceId IN (
                SELECT connector.Id FROM GraphConnectors connector
                JOIN GraphNodes node ON node.Id = connector.NodeId
                WHERE node.WorkflowId IN @workflowIds
            );
            """,
            new { workflowIds });

        Dictionary<Guid, (TasksGraph Graph, BaseGraphTask Task)> nodesById = [];
        foreach (var row in nodeRows)
        {
            if (!graphs.TryGetValue(row.WorkflowId, out TasksGraph? graph))
                continue;

            GraphNode node = row.ToNode();
            graph.Nodes.Add(node);

            // Only the nodes running something hold connectors, a group is drawn and nothing else.
            if (node is BaseGraphTask task)
                nodesById.Add(row.Id, (graph, task));
        }

        Dictionary<Guid, TasksGraph> graphsByConnectorId = [];
        foreach (var row in connectorRows)
        {
            if (!nodesById.TryGetValue(row.NodeId, out var node))
                continue;

            GraphConnector connector = row.ToConnector();
            (row.IsInput ? node.Task.Inputs : node.Task.Outputs).Add(connector);
            graphsByConnectorId[connector.Id] = node.Graph;
        }

        foreach (var row in connectionRows)
        {
            if (graphsByConnectorId.TryGetValue(row.SourceId, out TasksGraph? graph))
                graph.Connections.Add(row.ToConnection());
        }

        return graphs;
    }

    /// <summary>
    /// Write the graph of [workflow], whatever was stored of it before. The editor gives the graph
    /// back as a whole, holding the very ids that are stored, so the rows are dropped and written
    /// again rather than diffed : rewriting them lands on the same ones a diff would have.
    /// </summary>
    public static async Task ReplaceAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        AutomationWorkflow workflow)
    {
        // The connectors of the nodes, and the connections between them, go along with the nodes.
        await connection.ExecuteAsync(
            "DELETE FROM GraphNodes WHERE WorkflowId = @workflowId;",
            new { workflowId = workflow.Id },
            transaction);

        var nodes = workflow.Graph.Nodes;
        if (nodes.Count == 0)
            return;

        await connection.ExecuteAsync($"""
            INSERT INTO GraphNodes ({GraphNodeModel.Columns})
            VALUES ({GraphNodeModel.Values});
            """,
            nodes.Select(x => GraphNodeModel.From(x, workflow.Id)).ToList(),
            transaction);

        List<GraphConnectorModel> connectors = [];
        foreach (BaseGraphTask task in nodes.OfType<BaseGraphTask>())
        {
            connectors.AddRange(task.Inputs.Select((x, position) => GraphConnectorModel.From(x, task.Id, true, position)));
            connectors.AddRange(task.Outputs.Select((x, position) => GraphConnectorModel.From(x, task.Id, false, position)));
        }

        if (connectors.Count == 0)
            return;

        await connection.ExecuteAsync($"""
            INSERT INTO GraphConnectors ({GraphConnectorModel.Columns})
            VALUES ({GraphConnectorModel.Values});
            """,
            connectors,
            transaction);

        // A connection whose connectors are gone is left behind : the graph couldn't be refreshed
        // with it anyway, its connectors being resolved by id.
        HashSet<Guid> connectorIds = [.. connectors.Select(x => x.Id)];
        var connections = workflow.Graph.Connections
            .Where(x => connectorIds.Contains(x.SourceId) && connectorIds.Contains(x.TargetId))
            .Select(GraphConnectionModel.From)
            .ToList();

        if (connections.Count == 0)
            return;

        await connection.ExecuteAsync($"""
            INSERT INTO GraphConnections ({GraphConnectionModel.Columns})
            VALUES ({GraphConnectionModel.Values});
            """,
            connections,
            transaction);
    }
}
