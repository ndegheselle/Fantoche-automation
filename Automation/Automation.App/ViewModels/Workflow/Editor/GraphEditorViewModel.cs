using Automation.App.Shared.ViewModels.Work;
using Automation.App.Views.WorkPages.Workflows.Editor;
using System.Collections.ObjectModel;

namespace Automation.App.ViewModels.Workflow.Editor
{
    public class GraphEditorViewModel
    {
        public event Action<string>? AlertRaised;

        public GraphPendingConnection? PendingConnection { get; }
        public ObservableCollection<GraphNode> SelectedNodes { get; set; } = [];

        public Graph Graph { get; }
        public GraphEditor Editor { get; }
        public GraphEditorActions Actions { get; }
        public GraphEditorSettings Settings { get; }
        public GraphEditorCommands Commands { get; }

        public GraphEditorViewModel(GraphEditor editor, Graph graph, GraphEditorSettings settings)
        {
            Graph = graph;
            Editor = editor;
            Settings = settings;
            Actions = new GraphEditorActions(this);
            Commands = new GraphEditorCommands(this);
            PendingConnection = new GraphPendingConnection(this);
        }

        public void RaiseAlert(string message)
        {
            AlertRaised?.Invoke(message);
        }

        public void Connect(IEnumerable<GraphConnection> connections)
        {
            foreach (var connection in connections)
            {
                Graph.Connections.Add(connection);
            }
        }

        public void Disconnect(IEnumerable<GraphConnection> connections)
        {
            foreach (var connection in connections)
            {
                Graph.Connections.Remove(connection);
                // Refresh connector
                connection.Source.IsConnected = GetLinkedConnections(connection.Source).Any();
                connection.Target.IsConnected = GetLinkedConnections(connection.Target).Any();
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

        public IEnumerable<GraphConnection> GetLinkedConnections(TaskConnector connector)
        {
            return Graph.Connections.Where(x => x.SourceId == connector.Id || x.TargetId == connector.Id);
        }
    }
}
