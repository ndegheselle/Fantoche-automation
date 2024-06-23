using Automation.App.Base;
using Automation.Base;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Automation.App.ViewModels.Graph
{
    public class EditorViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public ICommand DisconnectConnectorCommand { get; }
        public PendingConnection? PendingConnection { get; }
        public WorkflowNode Workflow { get; set; }

        public EditorViewModel(WorkflowNode workflow)
        {
            Workflow = workflow;

            PendingConnection = new PendingConnection(this);
            DisconnectConnectorCommand = new DelegateCommand<NodeConnector>(connector =>
            {
                var connection = Workflow.Connections.First(x => x.Source == connector || x.Target == connector);
                connection.Source.IsConnected = false;
                connection.Target.IsConnected = false;
                Workflow.Connections.Remove(connection);
            });
        }

        public void Connect(NodeConnector source, NodeConnector target)
        {
            Workflow?.Connections.Add(new NodeConnection(source, target));
        }
    }

    public class PendingConnection
    {
        private readonly EditorViewModel _editor;
        private NodeConnector _source;

        public PendingConnection(EditorViewModel editor)
        {
            _editor = editor;
            StartCommand = new DelegateCommand<NodeConnector>(source => _source = source);
            FinishCommand = new DelegateCommand<NodeConnector>(target =>
            {
                if (target != null)
                    _editor.Connect(_source, target);
            });
        }

        public ICommand StartCommand { get; }
        public ICommand FinishCommand { get; }
    }
}
