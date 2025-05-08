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

        public List<GraphConnector> Inputs { get; set; } = [];
        public List<GraphConnector> Outputs { get; set; } = [];
    }

    public class GraphConnector
    {
        public Guid Id { get; set; }
        public bool IsConnected { get; set; }
    }

    public class GraphConnection
    {
        public Guid SourceId { get; set; }
        public Guid TargetId { get; set; }
    }

    public class Graph : IIdentifier
    {
        public Guid Id { get; set; }
        // Store directly in Workflow instead of a separated table ?
        public Guid WorkflowId { get; set; }
        public List<GraphConnection> Connections { get; set; } = [];
        public List<GraphNode> Nodes { get; private set; } = [];
    }
}
