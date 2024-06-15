using Automation.App.Base;
using Automation.App.ViewModels.Scopes;
using Nodify;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace Automation.App.ViewModels.Graph
{
    public class EditorViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public ICommand DisconnectConnectorCommand { get; }
        public PendingConnection? PendingConnection { get; }
        public WorkflowScope Workflow { get; set; }

        public EditorViewModel(WorkflowScope workflow)
        {
            Workflow = workflow;

            PendingConnection = new PendingConnection(this);
            DisconnectConnectorCommand = new DelegateCommand<ElementConnector>(connector =>
            {
                var connection = Workflow.Connections.First(x => x.Source == connector || x.Target == connector);
                connection.Source.IsConnected = false;  // This is not correct if there are multiple connections to the same connector
                connection.Target.IsConnected = false;
                Workflow.Connections.Remove(connection);
            });
        }

        public void Connect(ElementConnector source, ElementConnector target)
        {
            Workflow?.Connections.Add(new ElementConnection(source, target));
        }
    }

    public class ElementConnector : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public string Name { get; set; }
        public Point Anchor { get; set; }
        public bool IsConnected { get; set; }
    }

    public class ElementConnection : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public ElementConnection(ElementConnector source, ElementConnector target)
        {
            Source = source;
            Target = target;

            Source.IsConnected = true;
            Target.IsConnected = true;
        }

        public ElementConnector Source { get; set; }
        public ElementConnector Target { get; set; }
    }

    public class PendingConnection
    {
        private readonly EditorViewModel _editor;
        private ElementConnector _source;

        public PendingConnection(EditorViewModel editor)
        {
            _editor = editor;
            StartCommand = new DelegateCommand<ElementConnector>(source => _source = source);
            FinishCommand = new DelegateCommand<ElementConnector>(target =>
            {
                if (target != null)
                    _editor.Connect(_source, target);
            });
        }

        public ICommand StartCommand { get; }
        public ICommand FinishCommand { get; }
    }
}
