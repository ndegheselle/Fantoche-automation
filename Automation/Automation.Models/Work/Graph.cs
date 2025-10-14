using Automation.Shared.Data;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Text.Json.Serialization;

namespace Automation.Models.Work
{
    [JsonDerivedType(typeof(GraphTask), "task")]
    [JsonDerivedType(typeof(GraphGroup), "group")]
    public partial class GraphNode
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class GraphGroup : GraphNode
    {
        public Size Size { get; set; }
    }

    public class GraphTask : GraphNode
    {
        public new string Name { get => Metadata.Name; set => Metadata.Name = value; }
        public ScopedMetadata Metadata { get; set; }

        public Guid TaskId { get; set; }
        public string SettingsJson { get; set; } = string.Empty;

        public List<GraphConnector> Inputs { get; set; } = [];
        public List<GraphConnector> Outputs { get; set; } = [];

        public GraphTask(BaseAutomationTask task)
        {
            TaskId = task.Id;
            Metadata = task.Metadata;

            Inputs = task. Inputs.Select(x => new GraphConnector(x)).ToList();
            Outputs = task.Outputs.Select(x => new GraphConnector(x)).ToList();
        }
    }

    public class GraphControl : GraphTask
    {
        public GraphControl(BaseAutomationTask task) : base(task)
        {}
    }

    public class GraphWorkflow : GraphTask
    {
        public GraphWorkflow(BaseAutomationTask task) : base(task)
        { }
    }

    public partial class GraphConnector
    {
        public Guid Id { get; set; }
        public Guid TaskConnectorId { get; set; }
        [JsonIgnore]
        public bool IsConnected { get; set; }
        [JsonIgnore]
        public GraphTask? Parent { get; set; }

        public GraphConnector(TaskConnector connector)
        {
            TaskConnectorId = connector.Id;
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

            Source.IsConnected = true;
            Target.IsConnected = true;
        }
    }

    public class Graph
    {
        public ObservableCollection<GraphConnection> Connections { get; set; } = [];
        public ObservableCollection<GraphNode> Nodes { get; private set; } = [];

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
