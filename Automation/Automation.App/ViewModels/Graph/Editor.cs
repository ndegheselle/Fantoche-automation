using Automation.Shared;
using Automation.Shared.ViewModels;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Input;

namespace Automation.App.ViewModels.Graph
{
    public class EditorViewModel : INotifyPropertyChanged
    {
        public const uint GRID_DEFAULT_SIZE = 20;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action<string>? InvalidConnection;

        public List<INode> SelectedNodes { get; set; } = [];
        public ICommand DisconnectConnectorCommand { get; }
        public PendingConnection? PendingConnection { get; }
        public WorkflowNode Workflow { get; set; }

        public EditorViewModel(WorkflowNode workflow)
        {
            Workflow = workflow;

            PendingConnection = new PendingConnection(this);
            DisconnectConnectorCommand = new DelegateCommand<NodeConnector>(connector =>
            {
                connector.IsConnected = false;
                var connections = Workflow.Connections.Where(x => x.Source == connector || x.Target == connector);
                for (int i = connections.Count() - 1; i >= 0; i--)
                {
                    var connection = connections.ElementAt(i);
                    // Get opposite connector
                    NodeConnector oppositeConnector = connection.Source == connector ? connection.Target : connection.Source;
                    // Check if opposite connector is connected to another connector and if not, set IsConnected to false
                    var oppositeConnections = Workflow.Connections.Where(x => x.Source == oppositeConnector || x.Target == oppositeConnector);
                    if (oppositeConnections.Count() == 1)
                        oppositeConnector.IsConnected = false;

                    Workflow.Connections.Remove(connection);
                }
            });
        }

        public void Connect(NodeConnector source, NodeConnector target)
        {
            NodeConnection connection = new NodeConnection(Workflow, source, target);
            Workflow?.AddConnection(connection);
        }

        public void Disconnect(NodeConnection connection)
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

    public class PendingConnection
    {
        private readonly EditorViewModel _editor;
        private NodeConnector? _source;

        public PendingConnection(EditorViewModel editor)
        {
            _editor = editor;
            StartCommand = new DelegateCommand<NodeConnector>(source => _source = source);
            FinishCommand = new DelegateCommand<NodeConnector>(target =>
            {
                if (target == null || _source == null)
                    return;

                if (_source == target)
                {
                    _editor.TriggerInvalidConnection("Can't connect a connector to itself.");
                    return;
                }

                if (_source.Type != target.Type)
                {
                    _editor.TriggerInvalidConnection("Can't connect two connectors of different types.");
                    return;
                }

                if (_source is NodeOutput && target is NodeOutput)
                {
                    _editor.TriggerInvalidConnection("Can't connect two output connectors.");
                    return;
                }

                if (_source is NodeInput && target is NodeInput)
                {
                    _editor.TriggerInvalidConnection("Can't connect two input connectors.");
                    return;
                }


                // If target is output, swap source and target
                if (target is NodeOutput)
                {
                    var temp = _source;
                    _source = target;
                    target = temp;
                }

                // If already exists delete the connection
                var existingConnection = _editor.Workflow.Connections.FirstOrDefault(x => x.Source == _source && x.Target == target);
                if (existingConnection != null)
                {
                    _editor.Disconnect(existingConnection);
                    return;
                }

                _editor.Connect(_source, target);
            });
        }

        public ICommand StartCommand { get; }
        public ICommand FinishCommand { get; }
    }
}
