using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using System.Windows;

namespace Automation.Shared.ViewModels
{
    public class WorkflowNode : TaskNode
    {
        public ObservableCollection<NodeConnection> Connections { get; } = [];

        [JsonIgnore]
        public ObservableCollection<TaskNode> Tasks { get; set; } = [];
        [JsonIgnore]
        public ObservableCollection<NodeGroup> Groups { get; set; } = [];

        public void AddConnection(NodeConnection connection)
        {
            connection.ParentWorkflow = this;
            Connections.Add(connection);
        }
    }

    public class WorkflowRelation
    {
        public Guid WorkflowId { get; set; }
        public Guid NodeId { get; set; }
    }

    public partial class NodeConnection
    {
        public Guid ParentId { get; set; }
        public Guid SourceId { get; set; }
        public Guid TargetId { get; set; }

        [JsonIgnore]
        public WorkflowNode ParentWorkflow { get; set; }
        [JsonIgnore]
        public TaskConnector Source { get; set; }
        [JsonIgnore]
        public TaskConnector Target { get; set; }

        // Deserialization
        public NodeConnection() { }

        public NodeConnection(WorkflowNode parent, TaskConnector source, TaskConnector target)
        {
            ParentId = parent.Id;
            SourceId = source.Id;
            TargetId = target.Id;

            ParentWorkflow = parent;
            Source = source;
            Target = target;
        }
    }
}