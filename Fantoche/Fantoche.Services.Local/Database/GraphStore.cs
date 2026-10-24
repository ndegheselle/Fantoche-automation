using System.Data;
using Fantoche.Services.Local.Models;
using Fantoche.Shared.Data.Graph;
using Fantoche.Shared.Data.Scoped;
using Dapper;

namespace Fantoche.Services.Local.Database;

/// <summary>
/// The graph of a workflow, spread over the nodes, their connectors and the connections between
/// them : read and written as a whole, the three tables never making sense on their own.
/// <para>
/// They hold the latest version of each graph. Every save changing a graph adds a version, the one
/// before only keeping what turns the new one back into it (see <see cref="GraphChanges"/>) : a
/// past version is rebuilt from the latest one, which is never stored twice.
/// </para>
/// </summary>
internal static class GraphStore
{
    /// <summary>
    /// The latest graphs of [workflowIds] and their version, every workflow given one even when its
    /// graph is empty. The tables are read once for the whole set rather than once per workflow.
    /// </summary>
    public static async Task<Dictionary<Guid, (TasksGraph Graph, int Version)>> LoadAsync(
        IDbConnection connection,
        IReadOnlyCollection<Guid> workflowIds)
    {
        Dictionary<Guid, GraphSnapshot> snapshots = await LoadSnapshotsAsync(connection, null, workflowIds);
        Dictionary<Guid, int> versions = await LoadVersionsAsync(connection, null, workflowIds);
        return snapshots.ToDictionary(x => x.Key, x => (x.Value.ToGraph(), versions[x.Key]));
    }

    /// <summary>
    /// The graph of [workflowId] as it was at [version], rebuilt from the latest one : null when it
    /// never had that version.
    /// </summary>
    public static async Task<TasksGraph?> LoadVersionAsync(IDbConnection connection, Guid workflowId, int version)
    {
        int latest = (await LoadVersionsAsync(connection, null, [workflowId]))[workflowId];
        if (version < 1 || version > latest)
            return null;

        GraphSnapshot snapshot = (await LoadSnapshotsAsync(connection, null, [workflowId]))[workflowId];

        // From the latest one back : each version holds what turns the one after it into itself.
        var changes = await connection.QueryAsync<string>("""
            SELECT Changes FROM GraphVersions
            WHERE WorkflowId = @workflowId AND Version >= @version AND Changes IS NOT NULL
            ORDER BY Version DESC;
            """,
            new { workflowId, version });
        foreach (string change in changes)
            GraphChanges.FromJson(change).ApplyTo(snapshot);

        return snapshot.ToGraph();
    }

    /// <summary>Write the graph of [workflow], just created, as its first version.</summary>
    public static async Task<int> CreateAsync(IDbConnection connection, IDbTransaction transaction, AutomationWorkflow workflow)
    {
        await WriteAsync(connection, transaction, workflow.Id, GraphSnapshot.From(workflow.Graph, workflow.Id));
        await AddVersionAsync(connection, transaction, workflow.Id, 1);
        return 1;
    }

    /// <summary>
    /// Write the graph of [workflow] as its new version, the latest one being kept as what turns
    /// the new one back into it. Nothing is added when the graph didn't change. Returns the version
    /// the workflow is at.
    /// </summary>
    public static async Task<int> SaveAsync(IDbConnection connection, IDbTransaction transaction, AutomationWorkflow workflow)
    {
        GraphSnapshot stored = (await LoadSnapshotsAsync(connection, transaction, [workflow.Id]))[workflow.Id];
        GraphSnapshot saved = GraphSnapshot.From(workflow.Graph, workflow.Id);
        int latest = (await LoadVersionsAsync(connection, transaction, [workflow.Id]))[workflow.Id];

        GraphChanges back = GraphChanges.Between(saved, stored);
        if (back.IsEmpty)
            return latest;

        // A workflow stored before the versions has its graph as its first one, with no row yet.
        int updated = await connection.ExecuteAsync("""
            UPDATE GraphVersions SET Changes = @changes WHERE WorkflowId = @workflowId AND Version = @latest;
            """,
            new { changes = back.ToJson(), workflowId = workflow.Id, latest },
            transaction);
        if (updated == 0)
        {
            await connection.ExecuteAsync("""
                INSERT INTO GraphVersions (WorkflowId, Version, CreatedAt, Changes)
                VALUES (@workflowId, @latest, @createdAt, @changes);
                """,
                new { workflowId = workflow.Id, latest, createdAt = DateTime.UtcNow, changes = back.ToJson() },
                transaction);
        }

        await WriteAsync(connection, transaction, workflow.Id, saved);
        await AddVersionAsync(connection, transaction, workflow.Id, latest + 1);
        return latest + 1;
    }

    /// <summary>
    /// The latest version of each of [workflowIds] : 1 for a workflow stored before the versions,
    /// whose graph is its first one.
    /// </summary>
    private static async Task<Dictionary<Guid, int>> LoadVersionsAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        IReadOnlyCollection<Guid> workflowIds)
    {
        Dictionary<Guid, int> versions = workflowIds.ToDictionary(x => x, _ => 1);
        if (workflowIds.Count == 0)
            return versions;

        var rows = await connection.QueryAsync<GraphVersionModel>("""
            SELECT WorkflowId, MAX(Version) AS Version FROM GraphVersions
            WHERE WorkflowId IN @workflowIds
            GROUP BY WorkflowId;
            """,
            new { workflowIds },
            transaction);
        foreach (GraphVersionModel row in rows)
            versions[row.WorkflowId] = row.Version;
        return versions;
    }

    private static Task AddVersionAsync(IDbConnection connection, IDbTransaction transaction, Guid workflowId, int version)
        => connection.ExecuteAsync("""
            INSERT INTO GraphVersions (WorkflowId, Version, CreatedAt, Changes)
            VALUES (@workflowId, @version, @createdAt, NULL);
            """,
            new { workflowId, version, createdAt = DateTime.UtcNow },
            transaction);

    /// <summary>The latest graphs of [workflowIds] as the tables hold them, an empty one for each graph without a node.</summary>
    private static async Task<Dictionary<Guid, GraphSnapshot>> LoadSnapshotsAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        IReadOnlyCollection<Guid> workflowIds)
    {
        Dictionary<Guid, GraphSnapshot> snapshots = workflowIds.ToDictionary(x => x, _ => new GraphSnapshot());
        if (workflowIds.Count == 0)
            return snapshots;

        var nodeRows = await connection.QueryAsync<GraphNodeModel>($"""
            SELECT {GraphNodeModel.Columns} FROM GraphNodes WHERE WorkflowId IN @workflowIds;
            """,
            new { workflowIds },
            transaction);

        var connectorRows = await connection.QueryAsync<GraphConnectorModel>($"""
            SELECT {GraphConnectorModel.Columns} FROM GraphConnectors
            WHERE NodeId IN (SELECT Id FROM GraphNodes WHERE WorkflowId IN @workflowIds);
            """,
            new { workflowIds },
            transaction);

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
            new { workflowIds },
            transaction);

        Dictionary<Guid, StoredNode> nodesById = [];
        foreach (GraphNodeModel row in nodeRows)
        {
            if (!snapshots.TryGetValue(row.WorkflowId, out GraphSnapshot? snapshot))
                continue;

            StoredNode node = new(row, []);
            snapshot.Nodes[row.Id] = node;
            nodesById[row.Id] = node;
        }

        Dictionary<Guid, GraphSnapshot> snapshotsByConnectorId = [];
        foreach (GraphConnectorModel row in connectorRows)
        {
            if (!nodesById.TryGetValue(row.NodeId, out StoredNode? node))
                continue;

            node.Connectors.Add(row);
            snapshotsByConnectorId[row.Id] = snapshots[node.Node.WorkflowId];
        }

        foreach (GraphConnectionModel row in connectionRows)
        {
            if (snapshotsByConnectorId.TryGetValue(row.SourceId, out GraphSnapshot? snapshot))
                snapshot.Connections.Add(row);
        }

        return snapshots;
    }

    /// <summary>
    /// Write [snapshot] as the latest graph of [workflowId], whatever was stored of it before : the
    /// rows are dropped and written again, the changes being what the versions keep.
    /// </summary>
    private static async Task WriteAsync(IDbConnection connection, IDbTransaction transaction, Guid workflowId, GraphSnapshot snapshot)
    {
        // The connectors of the nodes, and the connections between them, go along with the nodes.
        await connection.ExecuteAsync(
            "DELETE FROM GraphNodes WHERE WorkflowId = @workflowId;",
            new { workflowId },
            transaction);

        if (snapshot.Nodes.Count == 0)
            return;

        await connection.ExecuteAsync($"""
            INSERT INTO GraphNodes ({GraphNodeModel.Columns})
            VALUES ({GraphNodeModel.Values});
            """,
            snapshot.Nodes.Values.Select(x => x.Node).ToList(),
            transaction);

        List<GraphConnectorModel> connectors = [.. snapshot.Nodes.Values.SelectMany(x => x.Connectors)];
        if (connectors.Count == 0)
            return;

        await connection.ExecuteAsync($"""
            INSERT INTO GraphConnectors ({GraphConnectorModel.Columns})
            VALUES ({GraphConnectorModel.Values});
            """,
            connectors,
            transaction);

        if (snapshot.Connections.Count == 0)
            return;

        await connection.ExecuteAsync($"""
            INSERT INTO GraphConnections ({GraphConnectionModel.Columns})
            VALUES ({GraphConnectionModel.Values});
            """,
            snapshot.Connections.ToList(),
            transaction);
    }
}
