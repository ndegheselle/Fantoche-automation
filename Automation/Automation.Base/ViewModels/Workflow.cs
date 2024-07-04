using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Automation.Base.ViewModels
{
    public class WorkflowNode : TaskNode
    {
        public ObservableCollection<NodeConnection> Connections { get; } = [];
        [JsonIgnore]
        public ObservableCollection<Node> Nodes { get; set; } = [];

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
            AllowedConnectorEdits = EnumConnectorsDirection.Out;
        }
    }

    public class WorkflowOutputNode : TaskNode
    {
        public WorkflowOutputNode() : base()
        {
            Name = "End";
            AllowedConnectorEdits = EnumConnectorsDirection.In;
        }
    }

    public class WorkflowRelation
    {
        public Guid WorkflowId { get; set; }
        public Guid NodeId { get; set; }
    }
}