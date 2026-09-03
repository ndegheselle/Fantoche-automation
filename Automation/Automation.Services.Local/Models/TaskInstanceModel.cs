using Automation.Shared.Data.Execution;
using Newtonsoft.Json.Linq;

namespace Automation.Services.Local.Models;

/// <summary>
/// One row of the instances table, mapped by hand : the JSON columns are read back as JToken,
/// and the state of an instance stamps its FinishedAt as it is assigned.
/// </summary>
internal sealed record TaskInstanceModel
{
    /// <summary>
    /// The columns making up a stored instance, in the order the insert writes them.
    /// </summary>
    public const string Columns =
        "Id, TaskId, NodeId, ParentInstanceId, NodeName, Parameters, Output, State, CreatedAt, FinishedAt";

    public static readonly string Schema = """
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

    public Guid Id { get; init; }
    public Guid TaskId { get; init; }
    public Guid? NodeId { get; init; }
    public Guid? ParentInstanceId { get; init; }
    public string NodeName { get; init; } = string.Empty;
    public string? Parameters { get; init; }
    public string? Output { get; init; }
    public EnumTaskState State { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? FinishedAt { get; init; }

    public TaskInstance ToInstance()
    {
        var instance = new TaskInstance()
        {
            Id = Id,
            TaskId = TaskId,
            NodeId = NodeId,
            ParentInstanceId = ParentInstanceId,
            NodeName = NodeName,
            Parameters = Parameters == null ? null : JToken.Parse(Parameters),
            Output = Output == null ? null : JToken.Parse(Output),
            CreatedAt = CreatedAt,
            State = State,
        };

        // Assigning the state stamps FinishedAt, the stored value has to stay the one of the run.
        instance.FinishedAt = FinishedAt;

        return instance;
    }

    public static TaskInstanceModel From(TaskInstance instance)
    {
        return new TaskInstanceModel()
        {
            Id = instance.Id,
            TaskId = instance.TaskId,
            NodeId = instance.NodeId,
            ParentInstanceId = instance.ParentInstanceId,
            NodeName = instance.NodeName,
            Parameters = instance.Parameters?.ToString(Newtonsoft.Json.Formatting.None),
            Output = instance.Output?.ToString(Newtonsoft.Json.Formatting.None),
            State = instance.State,
            CreatedAt = instance.CreatedAt,
            FinishedAt = instance.FinishedAt
        };
    }
}

