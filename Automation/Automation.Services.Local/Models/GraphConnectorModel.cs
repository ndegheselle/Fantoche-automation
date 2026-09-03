using Automation.Shared.Data.Graph;

namespace Automation.Services.Local.Models;

/// <summary>
/// One row of the graph connectors table : the endpoints a node is connected through, kept in the
/// order they are drawn in since an output branch is known by its place in the list.
/// </summary>
internal sealed record GraphConnectorModel
{
    public const string InputDirection = "input";
    public const string OutputDirection = "output";

    /// <summary>
    /// The columns making up a stored connector, in the order the insert writes them.
    /// </summary>
    public const string Columns = "Id, NodeId, Direction, Position, Name";

    /// <summary>
    /// The values of <see cref="Columns"/>, for the insert.
    /// </summary>
    public const string Values = "@Id, @NodeId, @Direction, @Position, @Name";

    /// <summary>
    /// The connectors of the nodes. They hang under their node, which takes them along when it
    /// goes, and the connections between them go with them in turn.
    /// </summary>
    public static readonly string Schema = """
        CREATE TABLE IF NOT EXISTS GraphConnectors (
            Id TEXT NOT NULL PRIMARY KEY,
            NodeId TEXT NOT NULL REFERENCES GraphNodes (Id) ON DELETE CASCADE,
            Direction TEXT NOT NULL,
            Position INTEGER NOT NULL,
            Name TEXT NOT NULL
        );

        CREATE INDEX IF NOT EXISTS IX_GraphConnectors_NodeId ON GraphConnectors (NodeId);
        """;

    public Guid Id { get; init; }
    public Guid NodeId { get; init; }
    public string Direction { get; init; } = InputDirection;

    /// <summary>Place of the connector among the inputs or the outputs of its node.</summary>
    public int Position { get; init; }
    public string Name { get; init; } = string.Empty;

    public bool IsInput => Direction == InputDirection;

    public GraphConnector ToConnector()
    {
        // The node holding it is set by TasksGraph.Refresh(), as for a graph read from anywhere else.
        return new GraphConnector() { Id = Id, Name = Name };
    }

    public static GraphConnectorModel From(GraphConnector connector, Guid nodeId, bool isInput, int position)
    {
        return new GraphConnectorModel()
        {
            Id = connector.Id,
            NodeId = nodeId,
            Direction = isInput ? InputDirection : OutputDirection,
            Position = position,
            Name = connector.Name,
        };
    }
}
