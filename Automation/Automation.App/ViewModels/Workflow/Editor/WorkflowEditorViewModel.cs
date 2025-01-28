using Automation.App.Shared.ApiClients;
using Automation.App.Shared.ViewModels.Work;
using Automation.Shared.Data;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Usuel.Shared;

namespace Automation.App.ViewModels.Workflow.Editor
{
    public class WorkflowEditorViewModel : INotifyPropertyChanged
    {
        public const uint GRID_DEFAULT_SIZE = 20;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string? propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event Action<string>? InvalidConnection;

        public List<LinkedNode> SelectedNodes { get; set; } = [];
        public ICommand DisconnectConnectorCommand { get; }
        public TaskPendingConnection? PendingConnection { get; }
        public WorkflowNode Workflow { get; }
        public WorkflowEditorSettings Settings { get; }
        public WorkflowEditorCommands Commands { get; }

        private readonly App _app = (App)System.Windows.Application.Current;
        private readonly TasksClient _nodeClient;

        public WorkflowEditorViewModel(WorkflowNode workflow, WorkflowEditorSettings settings)
        {
            _nodeClient = _app.ServiceProvider.GetRequiredService<TasksClient>();
            Workflow = workflow;
            Settings = settings;
            Commands = new WorkflowEditorCommands(this);

            PendingConnection = new TaskPendingConnection(this);
            DisconnectConnectorCommand = new DelegateCommand<TaskConnector>(connector =>
            {
                connector.IsConnected = false;
                var connections = Workflow.Connections.Where(x => x.Source == connector || x.Target == connector);
                for (int i = connections.Count() - 1; i >= 0; i--)
                {
                    var connection = connections.ElementAt(i);
                    // Get opposite connector
                    TaskConnector oppositeConnector = connection.Source == connector ? connection.Target : connection.Source;
                    // Check if opposite connector is connected to another connector and if not, set IsConnected to false
                    var oppositeConnections = Workflow.Connections.Where(x => x.Source == oppositeConnector || x.Target == oppositeConnector);
                    if (oppositeConnections.Count() == 1)
                        oppositeConnector.IsConnected = false;

                    Workflow.Connections.Remove(connection);
                }
            });
        }

        public void Connect(TaskConnector source, TaskConnector target)
        {
            TaskConnection connection = new TaskConnection(source, target);
            Workflow.Connections.Add(connection);
        }

        public void Disconnect(TaskConnection connection)
        {
            Workflow.Connections.Remove(connection);
            connection.Source.IsConnected = false;
            connection.Target.IsConnected = false;
        }

        public void TriggerInvalidConnection(string message)
        {
            InvalidConnection?.Invoke(message);
        }

        public void RemoveNodes(IList<LinkedNode> nodes)
        {
            for (int j = nodes.Count - 1; j >= 0; j--)
            {
                var node = nodes[j];
                Workflow.Nodes.Remove(node);
                var connections = Workflow.Connections.Where(x => x.Source.Parent.Id == node.Id || x.Target.Parent.Id == node.Id);
                for (int i = connections.Count() - 1; i >= 0; i--)
                {
                    var connection = connections.ElementAt(i);
                    Workflow.Connections.Remove(connection);
                }
            }
        }

        public async void AddNode(System.Windows.Point position, TaskNode task)
        {
            // TODO : Add the node to the center of the view
            TaskNode? addedTask = await _nodeClient.GetByIdAsync(task.Id);
            if (addedTask == null)
                return;

            Workflow.Nodes.Add(new RelatedTaskNode() { Position = position, Node = addedTask });
        }

        public void CreateGroup(Rectangle boundingBox)
        {
            // TODO : Create the group through the API
            NodeGroup group = new NodeGroup()
            {
                Position = new System.Windows.Point() { X =  boundingBox.Location.X, Y =  boundingBox.Location.Y },
                Size = new System.Windows.Size() { Width = boundingBox.Size.Width, Height = boundingBox.Size.Height }
            };
            Workflow.Nodes.Add(group);
        }
    }


    public class TaskPendingConnection
    {
        private readonly WorkflowEditorViewModel _editor;
        private TaskConnector? _source;

        public TaskPendingConnection(WorkflowEditorViewModel editor)
        {
            _editor = editor;
            StartCommand = new DelegateCommand<TaskConnector>(source => _source = source);
            FinishCommand = new DelegateCommand<TaskConnector>(target =>
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

                if (_source.Direction == target.Direction)
                {
                    _editor.TriggerInvalidConnection("Can't connect two output or input connectors.");
                    return;
                }

                // If target is output, swap source and target
                if (target.Direction == EnumTaskConnectorDirection.Out)
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
