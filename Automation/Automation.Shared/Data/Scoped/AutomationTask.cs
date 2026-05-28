using System.Text.Json.Serialization;
using Automation.Plugins.Shared;
using Automation.Shared.Data.Execution;
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
    [JsonIgnore]
    public JsonSchema? InputSchema
    {
        get => InputSchemaJson == null ? null : JsonSchema.FromJsonAsync(InputSchemaJson).Result;
        set => InputSchemaJson = value == null ? null : value.ToJson();
    }

    public string? InputSchemaJson { get; set; }

    [JsonIgnore]
    public JsonSchema? OutputSchema
    {
        get => OutputSchemaJson == null ? null : JsonSchema.FromJsonAsync(OutputSchemaJson).Result;
        set => OutputSchemaJson = value == null ? null : value.ToJson();
    }

    public string? OutputSchemaJson { get; set; }

    /// <summary>
    /// Named output branches the task can selectively activate (e.g. "true"/"false"
    /// for a condition). Empty when the task has a single default output.
    /// Mirrors <see cref="Automation.Plugins.Shared.ITask.OutputBranches"/>.
    /// </summary>
    public List<string> OutputBranches { get; set; } = [];

    public IEnumerable<Schedule> Schedules { get; set; } = [];

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
    public TaskTarget? Target { get; set; }

    public AutomationTask() : base(EnumScopedType.Task)
    {
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

        OutputBranches = packageTask.OutputBranches.ToList();
    }
}

public class AutomationControl : AutomationTask
{
    // Start and end task are special cases
    public static readonly AutomationControl StartTask = new AutomationControl(typeof(AutomationControl))
    {
        Id = Guid.Parse("00000000-0000-0000-0000-100000000001"),
        Metadata = new ScopedMetadata(EnumScopedType.Task) { Tags = ["Control"], Name = "Start", Icon = "\uE3D2", IsReadOnly = true },
        InputSchema = null,
        OutputSchema = new JsonSchema(),
    };
    public static readonly AutomationControl EndTask = new AutomationControl(typeof(AutomationControl))
    {
        Id = Guid.Parse("00000000-0000-0000-0000-100000000002"),
        Metadata = new ScopedMetadata(EnumScopedType.Task) { Tags = ["Control"], Name = "End", Icon = "\uE244", IsReadOnly = true },
        InputSchema = new JsonSchema(),
        OutputSchema = null
    };

    /// <summary>
    /// Type of the class that the target point on
    /// </summary>
    [JsonIgnore]
    public Type Type { get; set; }

    public AutomationControl(Type type)
    {
        Type = type;
    }
}