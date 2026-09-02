using Dapper;

namespace Automation.Services.Local;

/// <summary>
/// The tables the local database is made of, created when they are missing : the application
/// carries its own schema and there is no migration to run.
/// </summary>
public static class DatabaseSchema
{
    /// <summary>
    /// Create whatever is missing in the database. Called once, before anything reads or writes,
    /// the tables being created in the order they point at each other.
    /// </summary>
    public static void EnsureCreated(DatabaseFactory factory)
    {
        using var connection = factory.Create();
        connection.Execute(TaskInstancesTable);
    }

    /// <summary>
    /// History of what has been executed. An instance hangs under the task it ran and, for the node
    /// of a run, under the instance of the run itself : removing a task takes its whole history
    /// along. The connections come with their foreign keys enforced, so the table the instances
    /// point at has to be there before one is written.
    /// </summary>
    private const string TaskInstancesTable = """
        CREATE TABLE IF NOT EXISTS TaskInstances (
            Id TEXT NOT NULL PRIMARY KEY,
            TaskId TEXT NOT NULL REFERENCES Scoped (Id) ON DELETE CASCADE,
            NodeId TEXT NULL,
            ParentInstanceId TEXT NULL REFERENCES TaskInstances (Id) ON DELETE CASCADE,
            NodeName TEXT NOT NULL,
            Parameters TEXT NULL,
            Output TEXT NULL,
            State INTEGER NOT NULL,
            CreatedAt TEXT NOT NULL,
            FinishedAt TEXT NULL
        );

        CREATE INDEX IF NOT EXISTS IX_TaskInstances_TaskId ON TaskInstances (TaskId);
        CREATE INDEX IF NOT EXISTS IX_TaskInstances_ParentInstanceId ON TaskInstances (ParentInstanceId);
        CREATE INDEX IF NOT EXISTS IX_TaskInstances_CreatedAt ON TaskInstances (CreatedAt);
        """;
}
