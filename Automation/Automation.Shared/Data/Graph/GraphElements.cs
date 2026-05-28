using System.Drawing;
using System.Text.Json.Serialization;
using Automation.Shared.Data.Scoped;
using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace Automation.Shared.Data.Graph
{
    /// <summary>
    /// Base graph element
    /// </summary>
    [JsonDerivedType(typeof(GraphGroup), "group")]
    [JsonDerivedType(typeof(GraphTask), "task")]
    [JsonDerivedType(typeof(GraphControl), "control")]
    [JsonDerivedType(typeof(GraphWorkflow), "workflow")]
    public partial class GraphNode
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public virtual string Name { get; } = string.Empty;
    }

    /// <summary>
    /// Group nodes only visually
    /// </summary>
    public class GraphGroup : GraphNode
    {
        public Size Size { get; set; }
    }

    /// <summary>
    /// Settings of a task graph
    /// </summary>
    public class GraphTaskSettings
    {
        /// <summary>
        /// Wait for all inputs to complete before starting the task.
        /// </summary>
        public bool IsWaitingAllInputs { get; set; } = false;
    }

    /// <summary>
    /// Base graph task common to a task, workflow or control task
    /// </summary>
    [JsonDerivedType(typeof(GraphTask), "task")]
    [JsonDerivedType(typeof(GraphControl), "control")]
    [JsonDerivedType(typeof(GraphWorkflow), "workflow")]
    public class BaseGraphTask : GraphNode
    {
        public override string Name => Metadata.Name;
        public ScopedMetadata Metadata { get; set; } = new ScopedMetadata();

        public Guid TaskId { get; set; }

        [JsonIgnore]
        public BaseAutomationTask? AutomationTask { get; set; }

        public List<GraphConnector> Inputs { get; set; } = [];
        public List<GraphConnector> Outputs { get; set; } = [];

        /// <summary>
        /// Parameters template for the task — JSON with context references (e.g.
        /// <c>{{previous.X}}</c>, <c>{{context.Y}}</c>) that the executor resolves
        /// at runtime to populate <see cref="Execution.TaskInstance.Parameters"/>.
        /// Not the data flowing in from upstream tasks — that lives in the context.
        /// </summary>
        public string? ParametersJson { get; set; }
        public GraphTaskSettings Settings { get; set; } = new GraphTaskSettings();

        [JsonIgnore]
        public JsonSchema? InputSchema
        {
            get => InputSchemaJson == null ? null : JsonSchema.FromJsonAsync(InputSchemaJson).Result;
            set => InputSchemaJson = value?.ToJson();
        }

        public string? InputSchemaJson { get; set; }

        [JsonIgnore]
        public JsonSchema? OutputSchema
        {
            get => OutputSchemaJson == null ? null : JsonSchema.FromJsonAsync(OutputSchemaJson).Result;
            set => OutputSchemaJson = value?.ToJson();
        }

        public string? OutputSchemaJson { get; set; }

        public BaseGraphTask()
        {
        }

        public BaseGraphTask(BaseAutomationTask task)
        {
            TaskId = task.Id;
            Metadata = task.Metadata.Clone();

            InputSchema = task.InputSchema;
            OutputSchema = task.OutputSchema;
            Inputs = task.InputSchema == null ? [] : [new GraphConnector(this)];

            // One connector per declared output branch, or a single default connector
            // when the task didn't declare any. Tasks with no OutputSchema have no outputs.
            if (task.OutputSchema == null)
                Outputs = [];
            else if (task.OutputBranches.Count == 0)
                Outputs = [new GraphConnector(this)];
            else
                Outputs = task.OutputBranches.Select(name => new GraphConnector(this, name)).ToList();
        }
    }

    /// <summary>
    /// Graph task
    /// </summary>
    public class GraphTask : BaseGraphTask
    {
        public GraphTask()
        {
        }

        public GraphTask(AutomationTask task) : base(task)
        {
        }
    }

    /// <summary>
    /// Graph control task
    /// </summary>
    public class GraphControl : BaseGraphTask
    {
        public GraphControl()
        {
        }

        public GraphControl(AutomationControl task) : base(task)
        {
            // End wait for all inputs
            if (IsEnd())
                Settings.IsWaitingAllInputs = true;
        }

        public bool IsStart() => TaskId == AutomationControl.StartTask.Id;
        public bool IsEnd() => TaskId == AutomationControl.EndTask.Id;
    }

    /// <summary>
    /// Graph workflow (so a node representing a nested graph)
    /// </summary>
    public class GraphWorkflow : BaseGraphTask
    {
        public GraphWorkflow()
        {
        }

        public GraphWorkflow(AutomationWorkflow task) : base(task)
        {
        }
    }

    /// <summary>
    /// Endpoint of a graph node
    /// </summary>
    public partial class GraphConnector
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Branch name for output connectors — corresponds to one of
        /// <see cref="Automation.Plugins.Shared.ITask.OutputBranches"/>.
        /// Empty for the default single output / for inputs.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        [JsonIgnore] public bool IsConnected { get; set; }
        [JsonIgnore] public BaseGraphTask? Parent { get; set; }

        public GraphConnector(BaseGraphTask parent)
        {
            Parent = parent;
        }

        public GraphConnector(BaseGraphTask parent, string name) : this(parent)
        {
            Name = name;
        }
    }

    /// <summary>
    /// Connection between two connectors
    /// </summary>
    public class GraphConnection
    {
        public Guid SourceId { get; set; }
        public Guid TargetId { get; set; }

        [JsonIgnore] public GraphConnector? Source { get; set; }
        [JsonIgnore] public GraphConnector? Target { get; set; }

        public GraphConnection()
        {
        }

        public GraphConnection(GraphConnector source, GraphConnector target)
        {
            Connect(source, target);
        }

        public void Connect(GraphConnector source, GraphConnector target)
        {
            Source = source;
            Target = target;
            SourceId = Source.Id;
            TargetId = Target.Id;
            Source.IsConnected = true;
            Target.IsConnected = true;
        }
    }

    /// <summary>
    /// A task with the source downstream source of the task.
    /// </summary>
    public class GraphSource
    {
        public BaseGraphTask Task { get; set; }
        public GraphConnector SourceConnector { get; set; }
        public GraphSource(BaseGraphTask task, GraphConnector sourceConnector)
        {
            Task = task;
            SourceConnector = sourceConnector;
        }
    }
}
