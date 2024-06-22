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

    public class NodeWrapper : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Node Node { get; set; }
        public ScopeWrapper? Parent { get; set; }
        public bool IsExpanded { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;

                // Since we are using a treeview, we need to expand the parent when a child is selected (otherwise the selection will not go through)
                if (value)
                    ExpandParent();
                OnPropertyChanged();
            }
        }

        public NodeWrapper(Node node, ScopeWrapper? parent)
        {
            Node = node;
            Parent = parent;
        }

        public void ExpandParent()
        {
            if (Parent == null)
                return;

            Parent.IsExpanded = true;
            Parent.ExpandParent();
        }
    }
    public class ScopeWrapper : NodeWrapper
    {
        public new Scope Scope { get; }
        public new List<NodeWrapper> Childrens { get; }

        public ScopeWrapper(Scope scope) : base(scope, null)
        {
            Scope = scope;
            Childrens = scope.Childrens.Select(n => n is Scope s ? new ScopeWrapper(s) : new NodeWrapper(n, this)).ToList();
        }
    }
}
