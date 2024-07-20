using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using System.Windows;

namespace Automation.Shared.Data
{
    public class WorkflowNode : TaskNode
    {
        public List<TaskConnection> Connections { get; set; } = [];
        public List<TaskNode> Tasks { get; set; } = [];
        public List<NodeGroup> Groups { get; set; } = [];
    }

    public class WorkflowRelation
    {
        public Guid WorkflowId { get; set; }
        public Guid TaskId { get; set; }
    }

    public partial class TaskConnection
    {
        public Guid ParentId { get; set; }
        public Guid SourceId { get; set; }
        public Guid TargetId { get; set; }

        [JsonIgnore]
        public TaskConnector Source { get; set; }
        [JsonIgnore]
        public TaskConnector Target { get; set; }

        // Deserialization
        public TaskConnection() { }

        public TaskConnection(WorkflowNode parent, TaskConnector source, TaskConnector target)
        {
            ParentId = parent.Id;
            SourceId = source.Id;
            TargetId = target.Id;

            Source = source;
            Target = target;
        }
    }
}