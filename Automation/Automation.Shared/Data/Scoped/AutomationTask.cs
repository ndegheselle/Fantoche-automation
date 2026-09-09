using System.Text.Json.Serialization;
using Automation.Plugins.Shared;
using Automation.Shared.Data.Execution;
using Automation.Shared.Data;
using NJsonSchema;

namespace Automation.Shared.Data.Scoped;

public class TaskSettings
{
    public bool IsPassingThrough { get; set; } = false;
}

[JsonDerivedType(typeof(AutomationTask), "task")]
[JsonDerivedType(typeof(AutomationControl), "control")]
[JsonDerivedType(typeof(AutomationWorkflow), "workflow")]
public abstract class BaseAutomationTask : ScopedElement
{
    private JsonSchema? _inputSchema;
    private JsonSchema? _outputSchema;

    /// <summary>
    /// The shape of what the task reads, parsed once per value of <see cref="InputSchemaJson"/>
    /// (see <see cref="Schemas.Parse"/>).
    /// </summary>
    [JsonIgnore]
    public JsonSchema? InputSchema
    {
        get => _inputSchema ??= Schemas.Parse(InputSchemaJson);
        set
        {
            InputSchemaJson = value?.ToJson();
            _inputSchema = value;
        }
    }

    public string? InputSchemaJson
    {
        get;
        set
        {
            field = value;
            _inputSchema = null;
        }
    }

    /// <summary>
    /// The shape of what the task hands over, parsed once per value of
    /// <see cref="OutputSchemaJson"/> (see <see cref="Schemas.Parse"/>).
    /// </summary>
    [JsonIgnore]
    public JsonSchema? OutputSchema
    {
        get => _outputSchema ??= Schemas.Parse(OutputSchemaJson);
        set
        {
            OutputSchemaJson = value?.ToJson();
            _outputSchema = value;
        }
    }

    public string? OutputSchemaJson
    {
        get;
        set
        {
            field = value;
            _outputSchema = null;
        }
    }

    public List<Schedule> Schedules { get; set; } = [];

    public TaskSettings Settings { get; set; } = new();

    public BaseAutomationTask(EnumScopedType type) : base(type)
    {
    }

    public BaseAutomationTask(ScopedMetadata metadata) : base(metadata)
    {
    }
}

public class AutomationTask : BaseAutomationTask
{
    public PackageClassTarget? Target { get; set; }

    public AutomationTask() : base(EnumScopedType.Task)
    {
    }

    public AutomationTask(string name, Guid parentId) : base(new ScopedMetadata(name, EnumScopedType.Task))
    {
        ParentId = parentId;
    }

    public void UpdateFromTask(ITask packageTask)
    {
        if (packageTask.Output != null)
        {
            Settings.IsPassingThrough = packageTask.Output.Type == null;
            OutputSchema = packageTask.Output?.Type == null
                ? new JsonSchema()
                : JsonSchema.FromType(packageTask.Output.Type);
        }

        if (packageTask.Input != null)
            InputSchema = packageTask.Input?.Type == null
                ? new JsonSchema()
                : JsonSchema.FromType(packageTask.Input.Type);
    }
}

public class AutomationControl : AutomationTask
{
    // Start and end task are special cases
    public static readonly AutomationControl StartTask = new AutomationControl(typeof(AutomationControl))
    {
        Id = Guid.Parse("00000000-0000-0000-0000-100000000001"),
        ParentId = Scope.Controls.Id,
        Metadata = new ScopedMetadata(EnumScopedType.Task) { Tags = ["Control"], Name = "Start", Icon = "\ue13c", IsReadOnly = true },
        InputSchema = null,
        OutputSchema = new JsonSchema(),
    };
    public static readonly AutomationControl EndTask = new AutomationControl(typeof(AutomationControl))
    {
        Id = Guid.Parse("00000000-0000-0000-0000-100000000002"),
        ParentId = Scope.Controls.Id,
        Metadata = new ScopedMetadata(EnumScopedType.Task) { Tags = ["Control"], Name = "End", Icon = "\ue6b9", IsReadOnly = true },
        InputSchema = new JsonSchema(),
        OutputSchema = null
    };

    public static readonly AutomationControl ShareTask = new AutomationControl(typeof(AutomationControl))
    {
        Id = Guid.Parse("00000000-0000-0000-0000-100000000003"),
        ParentId = Scope.Controls.Id,
        Metadata = new ScopedMetadata(EnumScopedType.Task) { Tags = ["Control"], Name = "Share", Icon = "\ue36a", IsReadOnly = true },
        InputSchema = new JsonSchema(),
        OutputSchema = new JsonSchema(),
        Settings = new TaskSettings() { IsPassingThrough = true }
    };

    public static readonly AutomationControl JoinTask = new AutomationControl(typeof(AutomationControl))
    {
        Id = Guid.Parse("00000000-0000-0000-0000-100000000004"),
        ParentId = Scope.Controls.Id,
        Metadata = new ScopedMetadata(EnumScopedType.Task) { Tags = ["Control"], Name = "Join", Icon = "\ue43f", IsReadOnly = true },
        InputSchema = new JsonSchema(),
        OutputSchema = new JsonSchema(),
    };

    /// <summary>
    /// A join without the waiting : it reshapes what one branch produces into something the next
    /// ones read, and runs as many times as a branch reaches it.
    /// </summary>
    public static readonly AutomationControl MapTask = new AutomationControl(typeof(AutomationControl))
    {
        Id = Guid.Parse("00000000-0000-0000-0000-100000000005"),
        ParentId = Scope.Controls.Id,
        Metadata = new ScopedMetadata(EnumScopedType.Task) { Tags = ["Control"], Name = "Map", Icon = "\ue15e", IsReadOnly = true },
        InputSchema = new JsonSchema(),
        OutputSchema = new JsonSchema(),
    };

    /// <summary>
    /// Every control a graph knows on its own. They are hard coded rather than written by the user,
    /// so anything walking a graph tells them apart from the tasks with this list rather than with
    /// its own copy of their ids.
    /// </summary>
    public static readonly IReadOnlyList<AutomationControl> All = [StartTask, EndTask, ShareTask, JoinTask, MapTask];

    /// <summary>
    /// The control [taskId] stands for, null when it points at a task or a workflow.
    /// </summary>
    public static AutomationControl? Get(Guid taskId) => All.FirstOrDefault(x => x.Id == taskId);

    /// <summary>
    /// Type of the class that the target point on
    /// </summary>
    [JsonIgnore]
    public Type Type { get; set; }

    // Needed so persistence layers (e.g. EF Core) can materialize instances from storage.
    public AutomationControl() : this(typeof(AutomationControl))
    {
    }

    public AutomationControl(Type type)
    {
        Type = type;
    }
}