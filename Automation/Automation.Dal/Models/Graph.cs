using Automation.Shared.Data;
using MongoDB.Bson.Serialization.Attributes;
using System.Drawing;
using System.Text.Json.Serialization;

namespace Automation.Dal.Models
{
    [JsonDerivedType(typeof(GraphTask), "task")]
    [JsonDerivedType(typeof(GraphGroup), "group")]
    [BsonKnownTypes(typeof(GraphTask))]
    [BsonKnownTypes(typeof(GraphGroup))]
    public class GraphNode
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Point Position { get; set; }
    }

    public class GraphGroup : GraphNode
    {
        public Size Size { get; set; }
    }

    public class GraphTask : GraphNode
    {
        public string Icon { get; set; } = string.Empty;

        public Guid TaskId { get; set; }
        public string SettingsJson { get; set; } = string.Empty;

        public List<GraphConnector> Inputs { get; set; } = [];
        public List<GraphConnector> Outputs { get; set; } = [];
    }

    public class GraphWorkflow : GraphTask
    {}

    public class GraphConnector
    {
        public Guid Id { get; set; }
        public bool IsConnected { get; set; }

        [BsonIgnore, JsonIgnore]
        public GraphTask? Parent { get; set; }
    }

    public class GraphConnection
    {
        public Guid SourceId { get; set; }
        public Guid TargetId { get; set; }

        [BsonIgnore, JsonIgnore]
        public GraphConnector? Source { get; set; }
        [BsonIgnore, JsonIgnore]
        public GraphConnector? Target { get; set; }
    }

    public class Graph : IIdentifier
    {
        public Guid Id { get; set; }
        public Guid WorkflowId { get; set; }
        public List<GraphConnection> Connections { get; set; } = [];
        public List<GraphNode> Nodes { get; private set; } = [];

        /// <summary>
        /// Refresh parent and object references between TaskNode, Connection and Connectors.
        /// Simplify the graph resolution.
        /// </summary>
        public void Refresh()
        {
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
                connection.Source = source;
                connection.Target = source;
            }
        }
    }
}
