using Automation.Shared.Data;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using System.Windows;

namespace Automation.App.Shared.ViewModels.Tasks
{
    public class WorkflowNode : TaskNode
    {
        public ObservableCollection<TaskConnection> Connections { get; set; } = new ObservableCollection<TaskConnection>();
        public ObservableCollection<LinkedNode> Nodes { get; private set; } = new ObservableCollection<LinkedNode>();

        public WorkflowNode()
        {
            Type = EnumScopedType.Workflow;
        }

        public void RefreshConnections()
        {
            Dictionary<Guid, TaskConnector> connectors = new Dictionary<Guid, TaskConnector>();
            foreach (var node in Nodes)
            {
                if (node is RelatedTaskNode related)
                {
                    foreach (var connector in related.Node.Inputs)
                        connectors.Add(connector.Id, connector);
                    foreach (var connector in related.Node.Outputs)
                        connectors.Add(connector.Id, connector);
                }
            }

            // Set connections with corresponding connectors
            foreach (var connection in Connections)
            {
                var source = connectors[connection.SourceId];
                var target = connectors[connection.TargetId];
                connection.Connect(source, target);
            }
        }
    }

    // TODO : Derived json types
    public class LinkedNode : ILinkedNode
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Point Position { get; set; }
        System.Drawing.Point ILinkedNode.Position
        {
            get => new System.Drawing.Point((int)Position.X, (int)Position.Y);
            set => Position = new Point(value.X, value.Y);
        }
    }

    public class NodeGroup : LinkedNode
    {
        public Size Size { get; set; }
    }

    public class RelatedTaskNode : LinkedNode
    {
        public new string Name => Node.Name;
        public TaskNode Node { get; set; }
    }

    public class TaskConnection
    {
        public Guid ParentId { get; set; }

        public Guid SourceId { get; set; }

        public Guid TargetId { get; set; }

        public TaskConnector Source { get; set; }
        public TaskConnector Target { get; set; }

        public TaskConnection()
        {}

        public TaskConnection(TaskConnector source, TaskConnector target)
        {
            Connect(source, target);
        }

        public void Connect(TaskConnector source, TaskConnector target)
        {
            TargetId = source.Id;
            SourceId = source.Id;
            Source = source;
            Target = target;

            Source.IsConnected = true;
            Target.IsConnected = true;
        }
    }
}
