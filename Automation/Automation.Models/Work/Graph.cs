using Automation.Shared.Data;
using NJsonSchema;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Text.Json.Serialization;

namespace Automation.Models.Work
{
    [JsonDerivedType(typeof(GraphGroup), "group")]
    [JsonDerivedType(typeof(GraphTask), "task")]
    [JsonDerivedType(typeof(GraphControl), "control")]
    [JsonDerivedType(typeof(GraphWorkflow), "workflow")]
    public partial class GraphNode
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
    }

    public class GraphGroup : GraphNode
    {
        public Size Size { get; set; }
    }

    public class GraphTaskSetting
    {
        public bool WaitAll { get; set; } = false;
    }

    [JsonDerivedType(typeof(GraphTask), "task")]
    [JsonDerivedType(typeof(GraphControl), "control")]
    [JsonDerivedType(typeof(GraphWorkflow), "workflow")]
    public class BaseGraphTask : GraphNode
    {
        public new string Name
        {
            get => Metadata.Name;
            set => Metadata.Name = value;
        }

        public ScopedMetadata Metadata { get; set; } = new ScopedMetadata();

        public Guid TaskId { get; set; }

        public List<GraphConnector> Inputs { get; set; } = [];
        public List<GraphConnector> Outputs { get; set; } = [];

        public string? InputJson { get; set; }
        public GraphTaskSetting Settings { get; private set; } = new GraphTaskSetting();

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
            Metadata = task.Metadata;

            InputSchema = task.InputSchema;
            OutputSchema = task.OutputSchema;
            Inputs = task.InputSchema == null ? [] : [new GraphConnector(this)];
            Outputs = task.OutputSchema == null ? [] : [new GraphConnector(this)];
        }
    }

    public class GraphTask : BaseGraphTask
    {
        public GraphTask()
        {
        }

        public GraphTask(AutomationTask task) : base(task)
        {
        }
    }

    public class GraphControl : BaseGraphTask
    {
        public GraphControl()
        {
        }

        public GraphControl(AutomationControl task) : base(task)
        {
        }
    }

    public class GraphWorkflow : BaseGraphTask
    {
        public GraphWorkflow()
        {
        }

        public GraphWorkflow(AutomationWorkflow task) : base(task)
        {
        }
    }

    public partial class GraphConnector
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        [JsonIgnore] public bool IsConnected { get; set; }
        [JsonIgnore] public BaseGraphTask? Parent { get; set; }

        public GraphConnector(BaseGraphTask parent)
        {
            Parent = parent;
        }
    }

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

    public class Graph
    {
        public ObservableCollection<GraphConnection> Connections { get; set; } = [];
        public ObservableCollection<GraphNode> Nodes { get; set; } = [];

        private bool _isRefreshed;

        [JsonIgnore] public GraphExecutionContext Execution { get; private set; }

        public Graph()
        {
            Execution = new GraphExecutionContext(this);
        }

        /// <summary>
        /// Refresh parent and object references between TaskNode, Connection and Connectors.
        /// Simplify the graph resolution.
        /// </summary>
        /// <param name="force">Force the refresh even if the graph is already refreshed.</param>
        public void Refresh(bool force = false)
        {
            if (_isRefreshed && !force)
                return;

            var connectors = new Dictionary<Guid, GraphConnector>();
            foreach (GraphNode node in Nodes)
            {
                if (node is not BaseGraphTask taskNode)
                    continue;

                foreach (GraphConnector connector in taskNode.Inputs)
                {
                    connectors.Add(connector.Id, connector);
                    connector.Parent = taskNode;
                }

                foreach (GraphConnector connector in taskNode.Outputs)
                {
                    connectors.Add(connector.Id, connector);
                    connector.Parent = taskNode;
                }
            }

            // Set connections with corresponding connectors
            foreach (GraphConnection connection in Connections)
            {
                GraphConnector source = connectors[connection.SourceId];
                GraphConnector target = connectors[connection.TargetId];
                connection.Connect(source, target);
            }

            _isRefreshed = true;
        }

        #region Nodes

        public IEnumerable<GraphControl> GetStartNodes()
        {
            return Nodes.OfType<GraphControl>().Where(x => x.TaskId == AutomationControl.StartTaskId);
        }

        public IEnumerable<GraphControl> GetEndNodes()
        {
            return Nodes.OfType<GraphControl>().Where(x => x.TaskId == AutomationControl.EndTaskId);
        }

        #endregion
        
        #region Connections

        /// <summary>
        /// Get all previous tasks.
        /// </summary>
        /// <param name="task">Task to get the previous tasks from</param>
        /// <returns></returns>
        public IEnumerable<BaseGraphTask> GetPreviousFrom(BaseGraphTask task)
        {
            var connections = GetInputsConnectionsFrom(task);
            return connections.Select(x => x.Source!.Parent!);
        }

        /// <summary>
        /// Get all the connections linked to a task.
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        public IEnumerable<GraphConnection> GetConnectionsFrom(BaseGraphTask task)
        {
            List<GraphConnection> connections = [];
            connections.AddRange(GetInputsConnectionsFrom(task));
            connections.AddRange(GetOutputsConnectionsFrom(task));
            return connections;
        }

        /// <summary>
        /// Get all the input connections linked to a task.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<GraphConnection> GetInputsConnectionsFrom(BaseGraphTask task)
        {
            List<GraphConnection> connections = [];
            foreach (GraphConnector input in task.Inputs)
                connections.AddRange(GetConnectionsFrom(input));
            return connections;
        }

        /// <summary>
        /// Get all the output connections linked to a task.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<GraphConnection> GetOutputsConnectionsFrom(BaseGraphTask task)
        {
            List<GraphConnection> connections = [];
            foreach (GraphConnector input in task.Outputs)
                connections.AddRange(GetConnectionsFrom(input));
            return connections;
        }

        /// <summary>
        /// Get all the connections linked to a connector.
        /// </summary>
        /// <param name="connector"></param>
        /// <returns></returns>
        public IEnumerable<GraphConnection> GetConnectionsFrom(GraphConnector connector)
        {
            return Connections.Where(x => x.SourceId == connector.Id || x.TargetId == connector.Id);
        }

        #endregion
    }
}