using Automation.Shared.Data;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Automation.App.Shared.ViewModels.Work
{
    public class GraphNode
    {
        public string Name { get; set; } = string.Empty;
        public Point Position { get; set; }
    }

    public class GraphGroup : GraphNode
    {
        public Size Size { get; set; }
    }

    public class GraphTask : GraphNode
    {
        public string Icon { get; set; } = "\uf596";
        public List<GraphConnector> Inputs { get; set; } = [];
        public List<GraphConnector> Outputs { get; set; } = [];

        public GraphTask(AutomationTask task)
        {
            Name = task.Name;
            Icon = task.Icon;
            Inputs.Add(new GraphConnector());
            Outputs.Add(new GraphConnector());

            // TODO : get task inputs/outputs
        }
    }

    public class GraphConnector : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Guid Id { get; set; }
        public bool IsConnected { get; set; }
        public Point Anchor { get; set; }
        public EnumTaskConnectorType Type { get; set; }

        public GraphConnector(EnumTaskConnectorType type = EnumTaskConnectorType.Flow)
        {
            Type = type;
        }
    }

    public class GraphConnection
    {
        public Guid SourceId { get; set; }
        public Guid TargetId { get; set; }

        public GraphConnector Source { get; set; } = new GraphConnector();
        public GraphConnector Target { get; set; } = new GraphConnector();

        public GraphConnection()
        {}

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

        public void RefreshConnections()
        {
            Dictionary<Guid, GraphConnector> connectors = new Dictionary<Guid, GraphConnector>();
            foreach (var node in Nodes)
            {
                if (node is GraphTask related)
                {
                    foreach (var connector in related.Inputs)
                        connectors.Add(connector.Id, connector);
                    foreach (var connector in related.Outputs)
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
