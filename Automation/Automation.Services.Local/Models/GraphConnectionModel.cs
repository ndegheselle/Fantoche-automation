using Automation.Shared.Data.Graph;

namespace Automation.Services.Local.Models;

/// <summary>
/// One row of the graph connections table : an output connector linked to an input one. The pair is
/// the whole of it, which is also what a connection is identified by.
/// </summary>
internal sealed record GraphConnectionModel
{
    /// <summary>
    /// The columns making up a stored connection, in the order the insert writes them.
    /// </summary>
    public const string Columns = "SourceId, TargetId";

    /// <summary>
    /// The values of <see cref="Columns"/>, for the insert.
    /// </summary>
    public const string Values = "@SourceId, @TargetId";

    /// <summary>
    /// The connections of the graphs. They hang under the connectors they link : removing a node
    /// takes its connectors along, and with them whatever was connected to them.
    /// </summary>
    public static readonly string Schema = """
        CREATE TABLE IF NOT EXISTS GraphConnections (
            SourceId TEXT NOT NULL REFERENCES GraphConnectors (Id) ON DELETE CASCADE,
            TargetId TEXT NOT NULL REFERENCES GraphConnectors (Id) ON DELETE CASCADE,
            PRIMARY KEY (SourceId, TargetId)
        );

        CREATE INDEX IF NOT EXISTS IX_GraphConnections_TargetId ON GraphConnections (TargetId);
        """;

    public Guid SourceId { get; init; }
    public Guid TargetId { get; init; }

    public GraphConnection ToConnection()
    {
        // The connectors themselves are resolved by TasksGraph.Refresh(), only their ids are stored.
        return new GraphConnection() { SourceId = SourceId, TargetId = TargetId };
    }

    public static GraphConnectionModel From(GraphConnection connection)
    {
        return new GraphConnectionModel()
        {
            SourceId = connection.SourceId,
            TargetId = connection.TargetId,
        };
    }
}
