using Automation.Shared.Data;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace Automation.App.Shared.ViewModels.Work
{
    // TODO : Derived json types
    public class GraphNode : IGraphNode
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Point Position { get; set; }
        System.Drawing.Point IGraphNode.Position
        {
            get => new System.Drawing.Point((int)Position.X, (int)Position.Y);
            set => Position = new Point(value.X, value.Y);
        }
    }

    public class GraphGroup : GraphNode, IGraphGroup
    {
        public Size Size { get; set; }
        System.Drawing.Size IGraphGroup.Size
        {
            get => new System.Drawing.Size((int)Size.Width, (int)Size.Height);
            set => Size = new Size(value.Width, value.Height);
        }
    }

    public class GraphTask : GraphNode
    {
        public new string Name => Task.Name;
        public List<TaskConnector> Inputs => Task.Inputs;
        public List<TaskConnector> Outputs => Task.Outputs;

        public AutomationTask Task { get; set; } = new AutomationTask();

        public GraphTask(AutomationTask task)
        {
            Task = task;
        }
    }

    public class GraphConnection : IGraphConnection
    {
        public Guid SourceId { get; set; }
        public Guid TargetId { get; set; }

        public TaskConnector Source { get; set; } = new TaskConnector();
        public TaskConnector Target { get; set; } = new TaskConnector();

        public GraphConnection()
        {}

        public GraphConnection(TaskConnector source, TaskConnector target)
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

    public class Graph : IGraph
    {
        public Guid WorkflowId { get; set; }

        public ObservableCollection<GraphConnection> Connections { get; set; } = [];
        public ObservableCollection<GraphNode> Nodes { get; private set; } = [];
        public ObservableCollection<GraphGroup> Groups { get; private set; } = [];

        IEnumerable<IGraphConnection> IGraph.Connections => Connections;
        IEnumerable<IGraphNode> IGraph.Nodes => Nodes;
        IEnumerable<IGraphGroup> IGraph.Groups => Groups;

        public void RefreshConnections()
        {
            Dictionary<Guid, TaskConnector> connectors = new Dictionary<Guid, TaskConnector>();
            foreach (var node in Nodes)
            {
                if (node is GraphTask related)
                {
                    foreach (var connector in related.Task.Inputs)
                        connectors.Add(connector.Id, connector);
                    foreach (var connector in related.Task.Outputs)
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
}
