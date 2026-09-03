using System.Collections.ObjectModel;
using System.Drawing;
using Automation.Services.Local.Database;
using Automation.Shared.Data.Graph;
using Automation.Shared.Data.Scoped;

namespace Automation.Services.Local.Models;

/// <summary>
/// One row of the graph nodes table : the nodes of every workflow, each hanging under the one whose
/// graph holds it. A node points at the task it runs by id, which is what makes a task used.
/// </summary>
internal sealed record GraphNodeModel
{
    public const string GroupKind = "group";
    public const string TaskKind = "task";
    public const string ControlKind = "control";
    public const string WorkflowKind = "workflow";

    /// <summary>
    /// The columns making up a stored node, in the order the insert writes them.
    /// </summary>
    public const string Columns = """
        Id, WorkflowId, NodeKind, LocationX, LocationY, SizeWidth, SizeHeight,
        Name, Type, Color, Icon, IsReadOnly, Tags,
        TaskId, ParametersJson, InputSchemaJson, OutputSchemaJson
        """;

    /// <summary>
    /// The values of <see cref="Columns"/>, for the insert.
    /// </summary>
    public const string Values = """
        @Id, @WorkflowId, @NodeKind, @LocationX, @LocationY, @SizeWidth, @SizeHeight,
        @Name, @Type, @Color, @Icon, @IsReadOnly, @Tags,
        @TaskId, @ParametersJson, @InputSchemaJson, @OutputSchemaJson
        """;

    /// <summary>
    /// The nodes of the graphs. They hang under the workflow they belong to, so it takes them along
    /// when it goes. What they point at is a plain column : a task is dropped from under a graph
    /// only once nothing uses it anymore, which is checked before the removal rather than here.
    /// </summary>
    public static readonly string Schema = """
        CREATE TABLE IF NOT EXISTS GraphNodes (
            Id TEXT NOT NULL PRIMARY KEY,
            WorkflowId TEXT NOT NULL REFERENCES Scoped (Id) ON DELETE CASCADE,
            NodeKind TEXT NOT NULL,
            LocationX REAL NOT NULL,
            LocationY REAL NOT NULL,
            SizeWidth INTEGER NULL,
            SizeHeight INTEGER NULL,
            Name TEXT NULL,
            Type INTEGER NULL,
            Color TEXT NULL,
            Icon TEXT NULL,
            IsReadOnly INTEGER NULL,
            Tags TEXT NULL,
            TaskId TEXT NULL,
            ParametersJson TEXT NULL,
            InputSchemaJson TEXT NULL,
            OutputSchemaJson TEXT NULL
        );

        CREATE INDEX IF NOT EXISTS IX_GraphNodes_WorkflowId ON GraphNodes (WorkflowId);
        CREATE INDEX IF NOT EXISTS IX_GraphNodes_TaskId ON GraphNodes (TaskId);
        """;

    public Guid Id { get; init; }
    public Guid WorkflowId { get; init; }
    public string NodeKind { get; init; } = TaskKind;
    public double LocationX { get; init; }
    public double LocationY { get; init; }

    /// <summary>Groups only, which are the only nodes drawn with a size of their own.</summary>
    public int? SizeWidth { get; init; }
    public int? SizeHeight { get; init; }

    /// <summary>The metadata of a node running a task, a group having none.</summary>
    public string? Name { get; init; }
    public EnumScopedType? Type { get; init; }
    public string? Color { get; init; }
    public string? Icon { get; init; }
    public bool? IsReadOnly { get; init; }
    public string? Tags { get; init; }

    /// <summary>Nodes running a task, a workflow or a control.</summary>
    public Guid? TaskId { get; init; }
    public string? ParametersJson { get; init; }
    public string? InputSchemaJson { get; init; }
    public string? OutputSchemaJson { get; init; }

    public GraphNode ToNode()
    {
        GraphNode node = NodeKind switch
        {
            GroupKind => new GraphGroup() { Size = new Size(SizeWidth ?? 0, SizeHeight ?? 0) },
            TaskKind => new GraphTask(),
            ControlKind => new GraphControl(),
            WorkflowKind => new GraphWorkflow(),
            _ => throw new InvalidOperationException($"Unknown kind of graph node '{NodeKind}'."),
        };

        node.Id = Id;
        node.LocationX = LocationX;
        node.LocationY = LocationY;

        if (node is BaseGraphTask task)
        {
            task.TaskId = TaskId ?? Guid.Empty;
            task.Metadata = new ScopedMetadata(Name ?? "", Type ?? EnumScopedType.Task)
            {
                Color = Color,
                Icon = Icon,
                IsReadOnly = IsReadOnly ?? false,
                Tags = DatabaseJson.Deserialize<ObservableCollection<string>>(Tags) ?? [],
            };
            task.ParametersJson = ParametersJson;
            task.InputSchemaJson = InputSchemaJson;
            task.OutputSchemaJson = OutputSchemaJson;
        }

        return node;
    }

    public static GraphNodeModel From(GraphNode node, Guid workflowId)
    {
        var task = node as BaseGraphTask;
        var group = node as GraphGroup;

        return new GraphNodeModel()
        {
            Id = node.Id,
            WorkflowId = workflowId,
            NodeKind = KindOf(node),
            LocationX = node.LocationX,
            LocationY = node.LocationY,
            SizeWidth = group?.Size.Width,
            SizeHeight = group?.Size.Height,
            Name = task?.Metadata.Name,
            Type = task?.Metadata.Type,
            Color = task?.Metadata.Color,
            Icon = task?.Metadata.Icon,
            IsReadOnly = task?.Metadata.IsReadOnly,
            Tags = task == null ? null : DatabaseJson.Serialize(task.Metadata.Tags),
            TaskId = task?.TaskId,
            ParametersJson = task?.ParametersJson,
            InputSchemaJson = task?.InputSchemaJson,
            OutputSchemaJson = task?.OutputSchemaJson,
        };
    }

    private static string KindOf(GraphNode node) => node switch
    {
        GraphGroup => GroupKind,
        GraphControl => ControlKind,
        GraphWorkflow => WorkflowKind,
        GraphTask => TaskKind,
        _ => throw new InvalidOperationException($"'{node.GetType().Name}' can't be stored as a graph node."),
    };
}
