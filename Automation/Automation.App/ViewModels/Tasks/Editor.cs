using Automation.Shared;
using Automation.Shared.Data;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Input;

namespace Automation.App.ViewModels.Tasks
{
    public class EditorViewModel : INotifyPropertyChanged
    {
        public const uint GRID_DEFAULT_SIZE = 20;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action<string>? InvalidConnection;

        public List<INode> SelectedNodes { get; set; } = [];
        public ICommand DisconnectConnectorCommand { get; }
        public PendingConnection? PendingConnection { get; }
        public WorkflowWrapper Workflow { get; }

        public EditorViewModel(WorkflowNode workflow)
        {
            Workflow = new WorkflowWrapper(workflow);

            PendingConnection = new PendingConnection(this);
            DisconnectConnectorCommand = new DelegateCommand<NodifyConnector>(connector =>
            {
                connector.IsConnected = false;
                var connections = Workflow.Connections.Where(x => x.Source == connector || x.Target == connector);
                for (int i = connections.Count() - 1; i >= 0; i--)
                {
                    var connection = connections.ElementAt(i);
                    // Get opposite connector
                    NodifyConnector oppositeConnector = connection.Source == connector ? connection.Target : connection.Source;
                    // Check if opposite connector is connected to another connector and if not, set IsConnected to false
                    var oppositeConnections = Workflow.Connections.Where(x => x.Source == oppositeConnector || x.Target == oppositeConnector);
                    if (oppositeConnections.Count() == 1)
                        oppositeConnector.IsConnected = false;

                    Workflow.Connections.Remove(connection);
                }
            });
        }

        public void Connect(NodifyConnector source, NodifyConnector target)
        {
            NodifyConnection connection = new NodifyConnection(source, target);
            Workflow.Connections.Add(connection);
        }

        public void Disconnect(NodifyConnection connection)
        {
            Workflow.Connections.Remove(connection);
            connection.Source.IsConnected = false;
            connection.Target.IsConnected = false;
        }

        public void TriggerInvalidConnection(string message)
        {
            InvalidConnection?.Invoke(message);
        }

        public void RemoveNodes(IList<INode> nodes)
        {
            for (int j = nodes.Count - 1; j >= 0; j--)
            {
                var node = nodes[j];
                Workflow.Nodes.Remove(node);
                var connections = Workflow.Connections.Where(x => x.Source.Parent == node || x.Target.Parent == node);
                for (int i = connections.Count() - 1; i >= 0; i--)
                {
                    var connection = connections.ElementAt(i);
                    Workflow.Connections.Remove(connection);
                }
            }
        }

        public void CreateGroup(Rectangle boundingBox)
        {
            NodeGroup group = new NodeGroup()
            {
                Location = boundingBox.Location,
                Size = boundingBox.Size
            };
            Workflow.Nodes.Add(group);
        }
    }
}
