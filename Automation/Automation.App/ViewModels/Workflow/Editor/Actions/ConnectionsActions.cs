using Automation.App.Shared.Base.History;
using Automation.Models.Work;

namespace Automation.App.ViewModels.Workflow.Editor.Actions
{
    public class ConnectionsActions
    {
        public IReversibleCommand ConnectCommand { get; private set; }
        public IReversibleCommand DisconnectCommand { get; private set; }
        public IReversibleCommand DisconnectTaskCommand { get; private set; }

        private readonly GraphEditorViewModel _editor;
        private readonly HistoryHandler _history;
        public ConnectionsActions(GraphEditorViewModel editor, HistoryHandler history)
        {
            _editor = editor;
            _history = history;

            ConnectCommand = new ReversibleCommand<IEnumerable<GraphConnection>>(_history, OnConnect);
            DisconnectCommand = new ReversibleCommand<IEnumerable<GraphConnection>>(_history, OnDisconnect);
            DisconnectTaskCommand = new ReversibleCommand<GraphTask>(_history, OnDisconnect);
            ConnectCommand.Reverse = DisconnectCommand;
            DisconnectCommand.Reverse = ConnectCommand;
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
                connection.Source!.IsConnected = GetFrom(connection.Source).Any();
                connection.Target!.IsConnected = GetFrom(connection.Target).Any();
            }
        }
         
        public void Disconnect(GraphTask task) => DisconnectTaskCommand.Execute(task);
        private void OnDisconnect(GraphTask task)
        {
            var connections = GetFrom(task);
            Disconnect(connections);
        }

        private IEnumerable<GraphConnection> GetFrom(GraphTask task)
        {
            List<GraphConnection> connections = [];
            foreach (var input in task.Inputs)
                connections.AddRange(GetFrom(input));
            foreach (var output in task.Outputs)
                connections.AddRange(GetFrom(output));
            return connections;
        }

        private IEnumerable<GraphConnection> GetFrom(GraphConnector connector)
        {
            return _editor.Graph.Connections.Where(x => x.SourceId == connector.Id || x.TargetId == connector.Id);
        }
    }
}
