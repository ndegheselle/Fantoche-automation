using Automation.Shared.Data;
using NJsonSchema;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Text.Json.Serialization;
using Newtonsoft.Json.Linq;

namespace Automation.Models.Work
{
    [JsonDerivedType(typeof(GraphGroup), "group")]
    [JsonDerivedType(typeof(GraphTask), "task")]
    [JsonDerivedType(typeof(GraphControl), "control")]
    [JsonDerivedType(typeof(GraphWorkflow), "workflow")]
    public partial class GraphNode
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public virtual string Name { get; } = string.Empty;
    }

    public class GraphGroup : GraphNode
    {
        public Size Size { get; set; }
    }

    public class GraphTaskSettings
    {
        /// <summary>
        /// Wait for all inputs to complete before starting the task.
        /// </summary>
        public bool IsWaitingAllInputs { get; set; } = false;

        public bool IsPassingThrough { get; set; } = false;
    }

    [JsonDerivedType(typeof(GraphTask), "task")]
    [JsonDerivedType(typeof(GraphControl), "control")]
    [JsonDerivedType(typeof(GraphWorkflow), "workflow")]
    public class BaseGraphTask : GraphNode
    {
        public override string Name => Metadata.Name;
        public ScopedMetadata Metadata { get; set; } = new ScopedMetadata();

        public Guid TaskId { get; set; }

        public List<GraphConnector> Inputs { get; set; } = [];
        public List<GraphConnector> Outputs { get; set; } = [];

        public string? InputJson { get; set; }
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

        /// <summary>
        /// For Settings.WaitAll, we list inputs that are done to know then to start the task.
        /// Token are regrouped by node name.
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, JToken?> WaitedInputs { get; private set; } = new Dictionary<string, JToken?>();
        
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
            Outputs = task.OutputSchema == null ? [] : [new GraphConnector(this)];
            Settings.IsPassingThrough = task.Settings.IsPassingThrough;
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
            // End wait for all inputs
            if (IsEnd())
                Settings.IsWaitingAllInputs = true;
        }

        public bool IsEnd() => TaskId == AutomationControl.EndTaskId;
        public bool IsStart() => TaskId == AutomationControl.StartTaskId;
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

        [JsonIgnore] 
        public GraphExecutionContext Execution { get; private set; }

        [JsonIgnore]
        public Dictionary<Guid, BaseAutomationTask>? Tasks { get; set; }

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

        public IEnumerable<GraphControl> GetStartNodes() => Nodes.OfType<GraphControl>().Where(x => x.IsStart());
        public IEnumerable<GraphControl> GetEndNodes() => Nodes.OfType<GraphControl>().Where(x => x.IsEnd());

        /// <summary>
        /// Check if a task can execute (if all previous are completed for WaitAll)
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        public bool CanExecute(BaseGraphTask task)
        {
            // If we don't wait we can start the task immediately
            if (task.Settings.IsWaitingAllInputs == false)
                return true;
            
            // Else make sure that all previous task have finished
            var previousTasks = GetPreviousFrom(task);
            return previousTasks.All(previous => task.WaitedInputs.ContainsKey(previous.Name));
        }
        
        public string GetUniqueNodeName(string nodeName)
        {
            string uniqueName = nodeName;
            int count = 1;

            // Check if the name exists; if so, append a number and try again
            while (Nodes.Any(x => x.Name == uniqueName))
            {
                uniqueName = $"{nodeName} {count}";
                count++;
            }

            return uniqueName;
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
        /// Get all next tasks.
        /// </summary>
        /// <param name="task">Task to get the previous tasks from</param>
        /// <returns></returns>
        public IEnumerable<BaseGraphTask> GetNextFrom(BaseGraphTask task)
        {
            var connections = GetOutputsConnectionsFrom(task);
            return connections.Select(x => x.Target!.Parent!);
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