using Automation.Shared.Data;
using NJsonSchema;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Text.Json.Serialization;

namespace Automation.Models.Work
{
    [JsonDerivedType(typeof(GraphTask), "task")]
    [JsonDerivedType(typeof(GraphGroup), "group")]
    [JsonDerivedType(typeof(GraphWorkflow), "worklfow")]
    [JsonDerivedType(typeof(GraphControl), "control")]
    public partial class GraphNode
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
    }

    public class GraphGroup : GraphNode
    {
        public Size Size { get; set; }
    }

    public class BaseGraphTask : GraphNode
    {
        public new string Name { get => Metadata.Name; set => Metadata.Name = value; }
        public ScopedMetadata Metadata { get; set; } = new ScopedMetadata();

        public Guid TaskId { get; set; }

        public List<GraphConnector> Inputs { get; set; } = [];
        public List<GraphConnector> Outputs { get; set; } = [];

        public string? InputJson { get; set; }

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

        public BaseGraphTask()
        { }

        public BaseGraphTask(BaseAutomationTask task)
        {
            TaskId = task.Id;
            Metadata = task.Metadata;

            InputSchema = task.InputSchema;
            OutputSchema = task.OutputSchema;
            Inputs = task.InputSchema == null ? [] : [new GraphConnector(this)];
            Outputs = task.OutputSchema == null ? [] : [new GraphConnector(this)];
        }
    }

    public class GraphTask : BaseGraphTask
    {
        public GraphTask(AutomationTask task) : base(task)
        { }
    }

    public class GraphControl : BaseGraphTask
    {
        public GraphControl(AutomationControl task) : base(task)
        { }
    }

    public class GraphWorkflow : BaseGraphTask
    {
        public GraphWorkflow(AutomationWorkflow task) : base(task)
        { }
    }

    public partial class GraphConnector
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        [JsonIgnore]
        public bool IsConnected { get; set; }
        [JsonIgnore]
        public BaseGraphTask? Parent { get; set; }

        public GraphConnector(BaseGraphTask parent)
        {
            Parent = parent;
        }
    }

    public class GraphConnection
    {
        public Guid SourceId { get; set; }
        public Guid TargetId { get; set; }

        [JsonIgnore]
        public GraphConnector? Source { get; set; }
        [JsonIgnore]
        public GraphConnector? Target { get; set; }

        public GraphConnection()
        { }

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

    public class Graph
    {
        public ObservableCollection<GraphConnection> Connections { get; set; } = [];
        public ObservableCollection<GraphNode> Nodes { get; set; } = [];

        private bool _isRefreshed = false;
        /// <summary>
        /// Refresh parent and object references between TaskNode, Connection and Connectors.
        /// Simplify the graph resolution.
        /// </summary>
        /// <param name="force">Force the refresh even if the graph is already refreshed.</param>
        public void Refresh(bool force = false)
        {
            if (_isRefreshed == true && force != true)
                return;

            Dictionary<Guid, GraphConnector> connectors = new Dictionary<Guid, GraphConnector>();
            foreach (var node in Nodes)
            {
                if (node is GraphTask taskNode)
                {
                    foreach (var connector in taskNode.Inputs)
                    {
                        connectors.Add(connector.Id, connector);
                        connector.Parent = taskNode;
                    }
                    foreach (var connector in taskNode.Outputs)
                    {
                        connectors.Add(connector.Id, connector);
                        connector.Parent = taskNode;
                    }
                }
            }

            // Set connections with corresponding connectors
            foreach (var connection in Connections)
            {
                var source = connectors[connection.SourceId];
                var target = connectors[connection.TargetId];
                connection.Connect(source, target);
            }

            _isRefreshed = true;
        }
    }
}
