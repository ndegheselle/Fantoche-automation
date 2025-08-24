using Automation.App.Shared.Base.History;
using Automation.Dal.Models;

namespace Automation.App.ViewModels.Workflow.Editor.Actions
{
    public class ConnectionsActions
    {
        public IReversibleCommand ConnectCommand { get; private set; }
        public IReversibleCommand DisconnectCommand { get; private set; }

        private readonly GraphEditorViewModel _editor;
        private readonly HistoryHandler _history;
        public ConnectionsActions(GraphEditorViewModel editor, HistoryHandler history)
        {
            _editor = editor;
            _history = history;

            ConnectCommand = new ReversibleCommand<IEnumerable<GraphConnection>>(_history, OnConnect);
            DisconnectCommand = new ReversibleCommand<IEnumerable<GraphConnection>>(_history, OnDisconnect);
            ConnectCommand.Reverse = DisconnectCommand;
            DisconnectCommand.Reverse = DisconnectCommand;
        }

        public void Connect(IEnumerable<GraphConnection> connections) => ConnectCommand.Execute(connections);
        private void OnConnect(IEnumerable<GraphConnection> connections)
        {
            foreach (var connection in connections)
            {
                _editor.Graph.Connections.Add(connection);
            }
        }

        public void Disconnect(IEnumerable<GraphConnection> connections) => DisconnectCommand.Execute(connections);
        private void OnDisconnect(IEnumerable<GraphConnection> connections)
        {
            foreach (var connection in connections)
            {
                _editor.Graph.Connections.Remove(connection);
                // Refresh connector
                connection.Source!.IsConnected = GetLinkedConnections(connection.Source).Any();
                connection.Target!.IsConnected = GetLinkedConnections(connection.Target).Any();
            }
        }

        private IEnumerable<GraphConnection> GetLinkedConnections(GraphConnector connector)
        {
            return _editor.Graph.Connections.Where(x => x.SourceId == connector.Id || x.TargetId == connector.Id);
        }
    }
}
