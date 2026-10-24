namespace Fantoche.Services.Local.Models;

/// <summary>
/// One row of the graph versions table : a version of the graph of a workflow. The latest one is
/// the graph the node tables hold, every older one only keeps what turns the version after it back
/// into itself (see <see cref="Database.GraphChanges"/>).
/// </summary>
internal sealed record GraphVersionModel
{
    public static readonly string Schema = """
        CREATE TABLE IF NOT EXISTS GraphVersions (
            WorkflowId TEXT NOT NULL REFERENCES Scoped (Id) ON DELETE CASCADE,
            Version INTEGER NOT NULL,
            CreatedAt TEXT NOT NULL,
            Changes TEXT NULL,
            PRIMARY KEY (WorkflowId, Version)
        );
        """;

    public Guid WorkflowId { get; init; }
    public int Version { get; init; }
    public DateTime CreatedAt { get; init; }

    /// <summary>Null for the latest version : the graph itself is in the node tables.</summary>
    public string? Changes { get; init; }
}
