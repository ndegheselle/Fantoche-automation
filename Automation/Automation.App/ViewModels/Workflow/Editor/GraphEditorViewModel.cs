using Automation.App.Shared.ViewModels.Work;
using Automation.App.Views.WorkPages.Workflows.Editor;
using System.Collections.ObjectModel;

namespace Automation.App.ViewModels.Workflow.Editor
{
    public class GraphEditorViewModel
    {
        public event SimpleTargetedAction<string>? AlertRaised;

        public GraphPendingConnection? PendingConnection { get; }
        public ObservableCollection<GraphNode> SelectedNodes { get; set; } = [];

        public Graph Graph { get; }
        public GraphEditorCanvas Canvas { get; }
        public GraphEditorActions Actions { get; }
        public GraphEditorSettings Settings { get; }
        public GraphEditorCommands Commands { get; }

        public GraphEditorViewModel(Graph graph, GraphEditorCanvas canvas, GraphEditorSettings settings)
        {
            Graph = graph;
            Canvas = canvas;
            Settings = settings;
            Actions = new GraphEditorActions(this);
            Commands = new GraphEditorCommands(Actions, Canvas);
            PendingConnection = new GraphPendingConnection(this);
        }

        public void RaiseAlert(string message)
        {
            AlertRaised?.Invoke(message);
        }

        public void Connect(TaskConnector source, TaskConnector target)
        {
            GraphConnection connection = new GraphConnection(source, target);
            Graph.Connections.Add(connection);
        }

        public void Disconnect(GraphConnection connection)
        {
            Graph.Connections.Remove(connection);
            connection.Source.IsConnected = false;
            connection.Target.IsConnected = false;
        }

        public void DisconnectConnector(TaskConnector connector)
        {
            connector.IsConnected = false;
            var connections = Graph.Connections.Where(x => x.Source == connector || x.Target == connector);
            for (int i = connections.Count() - 1; i >= 0; i--)
            {
                var connection = connections.ElementAt(i);
                // Get opposite connector
                TaskConnector oppositeConnector = connection.Source == connector ? connection.Target : connection.Source;
                // Check if opposite connector is connected to another connector and if not, set IsConnected to false
                var oppositeConnections = Graph.Connections.Where(x => x.Source == oppositeConnector || x.Target == oppositeConnector);
                if (oppositeConnections.Count() == 1)
                    oppositeConnector.IsConnected = false;

                Graph.Connections.Remove(connection);
            }
        }

        public void AddNode(GraphNode node)
        {
            Graph.Nodes.Add(node);
        }

        public void RemoveNode(GraphNode node)
        {
            Graph.Nodes.Remove(node);
        }
    }
}
