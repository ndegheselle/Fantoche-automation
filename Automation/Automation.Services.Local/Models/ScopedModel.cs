using System.Collections.ObjectModel;
using Automation.Services.Local.Database;
using Automation.Shared.Data;
using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Scoped;

namespace Automation.Services.Local.Models;

/// <summary>
/// One row of the scoped elements table, mapped by hand : the whole tree shares a single table, the
/// kind of element telling which of its columns are filled. What an element holds without being
/// searched on (its settings, its schedules) is stored as JSON, the graph of a workflow having
/// tables of its own (see <see cref="GraphStore"/>).
/// </summary>
internal sealed record ScopedModel
{
    public const string ScopeKind = "scope";
    public const string TaskKind = "task";
    public const string ControlKind = "control";
    public const string WorkflowKind = "workflow";

    /// <summary>
    /// The columns making up a stored element, in the order the insert writes them.
    /// </summary>
    public const string Columns = """
        Id, ParentId, ElementKind, Name, Type, Color, Icon, IsReadOnly, Tags, ContextJson,
        InputSchemaJson, OutputSchemaJson, Schedules, Settings, Target,
        WorkflowSettings, OutputMappingJson, SharedSchemaJson
        """;

    /// <summary>
    /// The values of <see cref="Columns"/>, for the insert.
    /// </summary>
    public const string Values = """
        @Id, @ParentId, @ElementKind, @Name, @Type, @Color, @Icon, @IsReadOnly, @Tags, @ContextJson,
        @InputSchemaJson, @OutputSchemaJson, @Schedules, @Settings, @Target,
        @WorkflowSettings, @OutputMappingJson, @SharedSchemaJson
        """;

    /// <summary>
    /// Write an element, whatever it is : the columns its kind doesn't fill are simply left null.
    /// </summary>
    public const string InsertQuery = $"""
        INSERT INTO Scoped ({Columns})
        VALUES ({Values});
        """;

    /// <summary>
    /// Every column of <see cref="Columns"/> but the id, for the update : an element keeps the id it
    /// was created with, and changing kind isn't an edit but another element.
    /// </summary>
    public const string Assignments = """
        ParentId = @ParentId,
        Name = @Name,
        Type = @Type,
        Color = @Color,
        Icon = @Icon,
        IsReadOnly = @IsReadOnly,
        Tags = @Tags,
        ContextJson = @ContextJson,
        InputSchemaJson = @InputSchemaJson,
        OutputSchemaJson = @OutputSchemaJson,
        Schedules = @Schedules,
        Settings = @Settings,
        Target = @Target,
        WorkflowSettings = @WorkflowSettings,
        OutputMappingJson = @OutputMappingJson,
        SharedSchemaJson = @SharedSchemaJson
        """;

    /// <summary>
    /// The branch of the tree hanging under @elementId, the element itself included : the parent of
    /// an element is a plain column, so it is walked one level at a time.
    /// </summary>
    public const string BranchQuery = """
        WITH RECURSIVE Branch(Id) AS (
            SELECT Id FROM Scoped WHERE Id = @elementId
            UNION ALL
            SELECT child.Id FROM Scoped child JOIN Branch ON child.ParentId = Branch.Id
        )
        """;

    /// <summary>
    /// The tree of the scopes and the elements they hold. A child hangs under its parent, so
    /// removing a scope takes its whole branch along, and with it the history of what it ran.
    /// </summary>
    public static readonly string Schema = """
        CREATE TABLE IF NOT EXISTS Scoped (
            Id TEXT NOT NULL PRIMARY KEY,
            ParentId TEXT NULL REFERENCES Scoped (Id) ON DELETE CASCADE,
            ElementKind TEXT NOT NULL,
            Name TEXT NOT NULL,
            Type INTEGER NOT NULL,
            Color TEXT NULL,
            Icon TEXT NULL,
            IsReadOnly INTEGER NOT NULL,
            Tags TEXT NOT NULL,
            ContextJson TEXT NULL,
            InputSchemaJson TEXT NULL,
            OutputSchemaJson TEXT NULL,
            Schedules TEXT NULL,
            Settings TEXT NULL,
            Target TEXT NULL,
            WorkflowSettings TEXT NULL,
            OutputMappingJson TEXT NULL,
            SharedSchemaJson TEXT NULL
        );

        CREATE INDEX IF NOT EXISTS IX_Scoped_ParentId ON Scoped (ParentId);
        """;

    public Guid Id { get; init; }
    public Guid? ParentId { get; init; }
    public string ElementKind { get; init; } = ScopeKind;

    public string Name { get; init; } = string.Empty;
    public EnumScopedType Type { get; init; }
    public string? Color { get; init; }
    public string? Icon { get; init; }
    public bool IsReadOnly { get; init; }
    public string Tags { get; init; } = "[]";

    /// <summary>Scope only.</summary>
    public string? ContextJson { get; init; }

    /// <summary>Tasks, controls and workflows.</summary>
    public string? InputSchemaJson { get; init; }
    public string? OutputSchemaJson { get; init; }
    public string? Schedules { get; init; }
    public string? Settings { get; init; }

    /// <summary>Tasks and controls.</summary>
    public string? Target { get; init; }

    /// <summary>Workflows only.</summary>
    public string? WorkflowSettings { get; init; }
    public string? OutputMappingJson { get; init; }
    public string? SharedSchemaJson { get; init; }

    public ScopedElement ToElement()
    {
        ScopedElement element = ElementKind switch
        {
            ScopeKind => new Scope() { ContextJson = ContextJson },
            TaskKind => new AutomationTask(),
            ControlKind => new AutomationControl(),
            // The graph is read on its own, from the tables holding its nodes.
            WorkflowKind => new AutomationWorkflow()
            {
                WorkflowSettings = DatabaseJson.Deserialize<WorkflowSettings>(WorkflowSettings) ?? new WorkflowSettings(),
                OutputMappingJson = OutputMappingJson,
                SharedSchemaJson = SharedSchemaJson,
            },
            _ => throw new InvalidOperationException($"Unknown kind of scoped element '{ElementKind}'."),
        };

        element.Id = Id;
        element.ParentId = ParentId;
        element.Metadata = new ScopedMetadata(Name, Type)
        {
            Color = Color,
            Icon = Icon,
            IsReadOnly = IsReadOnly,
            Tags = DatabaseJson.Deserialize<ObservableCollection<string>>(Tags) ?? [],
        };

        if (element is BaseAutomationTask task)
        {
            task.InputSchemaJson = InputSchemaJson;
            task.OutputSchemaJson = OutputSchemaJson;
            task.Schedules = DatabaseJson.Deserialize<List<Schedule>>(Schedules) ?? [];
            task.Settings = DatabaseJson.Deserialize<TaskSettings>(Settings) ?? new TaskSettings();
        }

        // A control points at the class of a hard coded task, a plain task at one of a package.
        if (element is AutomationTask automationTask)
            automationTask.Target = DatabaseJson.Deserialize<PackageClassTarget>(Target);

        return element;
    }

    public static ScopedModel From(ScopedElement element)
    {
        var task = element as BaseAutomationTask;
        var workflow = element as AutomationWorkflow;

        return new ScopedModel()
        {
            Id = element.Id,
            ParentId = element.ParentId,
            ElementKind = KindOf(element),
            Name = element.Metadata.Name,
            Type = element.Metadata.Type,
            Color = element.Metadata.Color,
            Icon = element.Metadata.Icon,
            IsReadOnly = element.Metadata.IsReadOnly,
            Tags = DatabaseJson.Serialize(element.Metadata.Tags) ?? "[]",
            ContextJson = (element as Scope)?.ContextJson,
            InputSchemaJson = task?.InputSchemaJson,
            OutputSchemaJson = task?.OutputSchemaJson,
            Schedules = DatabaseJson.Serialize(task?.Schedules),
            Settings = DatabaseJson.Serialize(task?.Settings),
            Target = DatabaseJson.Serialize((element as AutomationTask)?.Target),
            WorkflowSettings = DatabaseJson.Serialize(workflow?.WorkflowSettings),
            OutputMappingJson = workflow?.OutputMappingJson,
            SharedSchemaJson = workflow?.SharedSchemaJson,
        };
    }

    private static string KindOf(ScopedElement element) => element switch
    {
        Scope => ScopeKind,
        // A control derives from a task : the most precise kind first.
        AutomationControl => ControlKind,
        AutomationWorkflow => WorkflowKind,
        AutomationTask => TaskKind,
        _ => throw new InvalidOperationException($"'{element.GetType().Name}' can't be stored as a scoped element."),
    };
}
