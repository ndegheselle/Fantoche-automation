using Automation.App.Base;
using Automation.Base;
using System.ComponentModel;
using System.Windows.Input;

namespace Automation.App.ViewModels.Graph
{
    public class EditorViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action? InvalidConnection;

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
            Workflow?.AddConnection(new NodeConnection(Workflow, source, target));
        }

        public void TriggerInvalidConnection()
        {
            InvalidConnection?.Invoke();
        }

        public void RemoveNode(TaskNode node)
        {
            Workflow.Tasks.Remove(node);
            var connections = Workflow.Connections.Where(x => x.Source.Parent == node || x.Target.Parent == node);
            for (int i = connections.Count() - 1; i >= 0; i--)
            {
                var connection = connections.ElementAt(i);
                Workflow.Connections.Remove(connection);
            }
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

                if (_source == target ||
                (_source is NodeOutput && target is NodeOutput) ||
                (_source is NodeInput && target is NodeInput))
                {
                    _editor.TriggerInvalidConnection();
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
                    _editor.Workflow.Connections.Remove(existingConnection);
                    _source.IsConnected = false;
                    target.IsConnected = false;
                    return;
                }

                _editor.Connect(_source, target);
            });
        }

        public ICommand StartCommand { get; }
        public ICommand FinishCommand { get; }
    }
}
