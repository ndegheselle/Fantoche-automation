using Fantoche.Shared.Data.Execution;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fantoche.Services.Local.Models;

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
        "Id, TaskId, WorkflowVersion, NodeId, ParentInstanceId, PreviousIds, NodeName, Shared, Parameters, Output, GlobalContext, ApplicationId, SessionId, DefinitionHash, State, CreatedAt, FinishedAt";

    /// <summary>The parameters of an insert, in the order of <see cref="Columns"/>.</summary>
    public const string Values =
        "@Id, @TaskId, @WorkflowVersion, @NodeId, @ParentInstanceId, @PreviousIds, @NodeName, @Shared, @Parameters, @Output, @GlobalContext, @ApplicationId, @SessionId, @DefinitionHash, @State, @CreatedAt, @FinishedAt";

    public static readonly string Schema = """
        CREATE TABLE IF NOT EXISTS TaskInstances (
            Id TEXT NOT NULL PRIMARY KEY,
            TaskId TEXT NOT NULL REFERENCES Scoped (Id) ON DELETE CASCADE,
            WorkflowVersion INTEGER NULL,
            NodeId TEXT NULL,
            ParentInstanceId TEXT NULL REFERENCES TaskInstances (Id) ON DELETE CASCADE,
            PreviousIds TEXT NOT NULL,
            NodeName TEXT NOT NULL,
            Shared TEXT NULL,
            Parameters TEXT NULL,
            Output TEXT NULL,
            GlobalContext TEXT NULL,
            ApplicationId TEXT NULL,
            SessionId TEXT NULL,
            DefinitionHash TEXT NULL,
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
    public int? WorkflowVersion { get; init; }
    public Guid? NodeId { get; init; }
    public Guid? ParentInstanceId { get; init; }
    /// <summary>A JSON array of the ids.</summary>
    public string PreviousIds { get; init; } = "[]";
    public string NodeName { get; init; } = string.Empty;
    public string? Shared { get; init; }
    public string? Parameters { get; init; }
    public string? Output { get; init; }
    public string? GlobalContext { get; init; }
    public string? ApplicationId { get; init; }
    public string? SessionId { get; init; }
    public string? DefinitionHash { get; init; }
    public EnumTaskState State { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? FinishedAt { get; init; }

    public TaskInstance ToInstance()
    {
        var instance = new TaskInstance()
        {
            Id = Id,
            TaskId = TaskId,
            WorkflowVersion = WorkflowVersion,
            NodeId = NodeId,
            ParentInstanceId = ParentInstanceId,
            PreviousIds = JsonConvert.DeserializeObject<List<Guid>>(PreviousIds) ?? [],
            NodeName = NodeName,
            Shared = Parse(Shared),
            Parameters = Parse(Parameters),
            Output = Parse(Output),
            GlobalContext = Parse(GlobalContext),
            ApplicationId = ApplicationId,
            SessionId = SessionId,
            DefinitionHash = DefinitionHash,
            CreatedAt = AsUtc(CreatedAt),
            State = State,
        };

        // Assigning the state stamps FinishedAt, the stored value has to stay the one of the run.
        instance.FinishedAt = FinishedAt == null ? null : AsUtc(FinishedAt.Value);

        return instance;
    }

    public static TaskInstanceModel From(TaskInstance instance)
    {
        return new TaskInstanceModel()
        {
            Id = instance.Id,
            TaskId = instance.TaskId,
            WorkflowVersion = instance.WorkflowVersion,
            NodeId = instance.NodeId,
            ParentInstanceId = instance.ParentInstanceId,
            PreviousIds = JsonConvert.SerializeObject(instance.PreviousIds),
            NodeName = instance.NodeName,
            Shared = Text(instance.Shared),
            Parameters = Text(instance.Parameters),
            Output = Text(instance.Output),
            GlobalContext = Text(instance.GlobalContext),
            ApplicationId = instance.ApplicationId,
            SessionId = instance.SessionId,
            DefinitionHash = instance.DefinitionHash,
            State = instance.State,
            CreatedAt = instance.CreatedAt,
            FinishedAt = instance.FinishedAt
        };
    }

    private static JToken? Parse(string? json) => json == null ? null : JToken.Parse(json);

    /// <summary>
    /// The instances are dated in UTC, which SQLite reads back as a date of no kind : taken as local
    /// time, the storage of a resumed run would be looked for under another name.
    /// </summary>
    private static DateTime AsUtc(DateTime date) => DateTime.SpecifyKind(date, DateTimeKind.Utc);

    private static string? Text(JToken? token) => token?.ToString(Formatting.None);
}
