using Automation.Plugins.Shared;
using Automation.Shared.Data;
using NJsonSchema;
using System.Text.Json.Serialization;

namespace Automation.Models.Work;

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

    public IEnumerable<Schedule> Schedules { get; set; } = [];

    public virtual TaskSettings Settings { get; set; } = new();

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
    }
}

public class AutomationControl : AutomationTask
{
    /// <summary>
    /// Id of the start task.
    /// </summary>
    public static readonly Guid StartTaskId = Guid.Parse("00000000-0000-0000-0000-100000000001");

    /// <summary>
    /// Id of the end task.
    /// </summary>
    public static readonly Guid EndTaskId = Guid.Parse("00000000-0000-0000-0000-100000000002");

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

public class WorkflowSettings
{
    public bool IsWaitingForAllEnd { get; set; }
}

public class AutomationWorkflow : BaseAutomationTask
{
    /// <summary>
    /// Store all data (input, output, global, ...) in task instance.
    /// </summary>
    public bool IsStoringAllData { get; set; } = false;

    public WorkflowSettings WorkflowSettings { get; set; } = new();

    public Graph Graph { get; set; } = new();

    /// <summary>
    /// Mapping for the output of the workflow.
    /// </summary>
    public string? OutputMappingJson { get; set; }

    /// <summary>
    /// Schema of all the common data of the workflow.
    /// </summary>
    public string? CommonSchemaJson { get; set; }

    public AutomationWorkflow() : base(EnumScopedType.Workflow)
    {
    }
}