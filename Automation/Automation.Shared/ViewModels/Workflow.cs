using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using System.Windows;

namespace Automation.Shared.ViewModels
{
    public class WorkflowNode : TaskNode
    {
        public ObservableCollection<NodeConnection> Connections { get; } = [];
        [JsonIgnore]
        public ObservableCollection<INode> Nodes { get; set; } = [];

        public void AddConnection(NodeConnection connection)
        {
            connection.ParentWorkflow = this;
            Connections.Add(connection);
        }
    }

    public class WorkflowInputNode : TaskNode
    {
        public WorkflowInputNode() : base()
        {
            Name = "Start";
        }
    }

    public class WorkflowOutputNode : TaskNode
    {
        public WorkflowOutputNode() : base()
        {
            Name = "End";
        }
    }

    public class WorkflowRelation
    {
        public Guid WorkflowId { get; set; }
        public Guid NodeId { get; set; }
    }
}