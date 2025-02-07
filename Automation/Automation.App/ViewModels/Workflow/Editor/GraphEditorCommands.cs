using Automation.App.Shared.ViewModels.Work;
using System.ComponentModel;
using System.Windows.Input;
using Usuel.Shared;

namespace Automation.App.ViewModels.Workflow.Editor
{
    public class GraphEditorCommands
    {
        private readonly Graph _graph;

        public ICommand ZoomIn { get; private set; }
        public ICommand ZoomOut { get; private set; }
        public ICommand ZoomFit { get; private set; }

        public ICommand DeleteSelection { get; private set; }
        public ICommand GroupSelection { get; private set; }

        public ICommand HistoryPrevious { get; private set; }
        public ICommand HistoryNext { get; private set; }
        public ICommand Save { get; private set; }

        public ICommand DisconnectConnectorCommand { get; private set; }

        public GraphEditorCommands(Graph graph)
        {
            _graph = graph;

            DisconnectConnectorCommand = new DelegateCommand<TaskConnector>(DisconnectConnector);
        }

        private void DisconnectConnector(TaskConnector connector)
        {
            connector.IsConnected = false;
            var connections = _graph.Connections.Where(x => x.Source == connector || x.Target == connector);
            for (int i = connections.Count() - 1; i >= 0; i--)
            {
                var connection = connections.ElementAt(i);
                // Get opposite connector
                TaskConnector oppositeConnector = connection.Source == connector ? connection.Target : connection.Source;
                // Check if opposite connector is connected to another connector and if not, set IsConnected to false
                var oppositeConnections = _graph.Connections.Where(x => x.Source == oppositeConnector || x.Target == oppositeConnector);
                if (oppositeConnections.Count() == 1)
                    oppositeConnector.IsConnected = false;

                _graph.Connections.Remove(connection);
            }
        }
    }
}
