using Accessibility;
using Automation.Models.Work;
using Usuel.History;

namespace Automation.App.ViewModels.Workflow.Editor.Actions
{
    public class ConnectionsActions
    {
        public ReversibleCommand<IEnumerable<GraphConnection>> ConnectCommand { get; private set; }
        public ReversibleCommand<IEnumerable<GraphConnection>> DisconnectCommand { get; private set; }
        public IReversibleCommand DisconnectTaskCommand { get; private set; }

        private readonly GraphEditorViewModel _editor;
        private readonly HistoryHandler _history;
        public ConnectionsActions(GraphEditorViewModel editor, HistoryHandler history)
        {
            _editor = editor;
            _history = history;

            ConnectCommand = new ReversibleCommand<IEnumerable<GraphConnection>>(_history, Connect);
            DisconnectCommand = new ReversibleCommand<IEnumerable<GraphConnection>>(_history, Disconnect);
            DisconnectTaskCommand = new ReversibleCommand<GraphTask>(_history, Disconnect);

            _history.SetReverse(ConnectCommand, DisconnectTaskCommand);
        }

        public void Connect(IEnumerable<GraphConnection> connections)
        {
            foreach (var connection in connections)
            {
                _editor.Graph.Connections.Add(connection);
            }
        }

        public void Disconnect(IEnumerable<GraphConnection> connections)
        {
            foreach (var connection in connections)
            {
                _editor.Graph.Connections.Remove(connection);
                // Refresh connector
                connection.Source!.IsConnected = _editor.Graph.GetConnectionsFrom(connection.Source).Any();
                connection.Target!.IsConnected = _editor.Graph.GetConnectionsFrom(connection.Target).Any();
            }
        }

        public void Disconnect(GraphTask task)
        {
            var connections = _editor.Graph.GetConnectionsFrom(task);
            Disconnect(connections);
        }
    }
}
