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
                connection.Source!.IsConnected = GetFrom(connection.Source).Any();
                connection.Target!.IsConnected = GetFrom(connection.Target).Any();
            }
        }

        public void Disconnect(GraphTask task)
        {
            var connections = GetFrom(task);
            Disconnect(connections);
        }

        public IEnumerable<GraphConnection> GetFrom(GraphTask task)
        {
            List<GraphConnection> connections = [];
            foreach (var input in task.Inputs)
                connections.AddRange(GetFrom(input));
            foreach (var output in task.Outputs)
                connections.AddRange(GetFrom(output));
            return connections;
        }

        public IEnumerable<GraphConnection> GetFrom(GraphConnector connector)
        {
            return _editor.Graph.Connections.Where(x => x.SourceId == connector.Id || x.TargetId == connector.Id);
        }
    }
}
