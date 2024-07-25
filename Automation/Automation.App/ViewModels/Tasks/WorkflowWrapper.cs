using Automation.Shared;
using Automation.Shared.Data;
using Nodify;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Automation.App.ViewModels.Tasks
{
    public class NodifyConnector : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public TaskConnector TaskConnector { get; set; }
        public NodifyNode Parent { get; set; }

        public string Name { get; set; }
        public bool IsConnected { get; set; }
        public Point Anchor { get; set; }

        public NodifyConnector(NodifyNode parent, TaskConnector taskConnector)
        {
            Parent = parent;
            TaskConnector = taskConnector;
            Name = taskConnector.Name;
        }
    }

    public class NodifyConnection
    {
        public NodifyConnector Source { get; set; }
        public NodifyConnector Target { get; set; }

        public NodifyConnection(NodifyConnector source, NodifyConnector target)
        {
            Source = source;
            Target = target;
            source.IsConnected = true;
            target.IsConnected = true;
        }
    }

    public interface INode
    {
        public string Name { get; set; }
        public Point Position { get; set; }
    }

    public class NodifyNode : INode
    {
        public WorkflowRelation Relation { get; set; }

        public string Name { get; set; }
        public Point Position { get; set; }

        public ObservableCollection<NodifyConnector> Inputs { get; set; } = [];
        public ObservableCollection<NodifyConnector> Outputs { get; set; } = [];

        public NodifyNode(WorkflowRelation relation, TaskNode task)
        {
            Relation = relation;
            Name = task.Name;
            Position = new Point(relation.TaskPosition.X, relation.TaskPosition.Y);

            foreach (var connector in task.Connectors)
            {
                var nodifyConnector = new NodifyConnector(this, connector);
                if (connector.Direction == EnumTaskConnectorDirection.In)
                    Inputs.Add(nodifyConnector);
                else
                    Outputs.Add(nodifyConnector);
            }
        }
    }

    public class NodifyGroup : INode
    {
        public NodeGroup NodeGroup { get; set; }

        public string Name { get; set; }
        public Point Position { get; set; }
        public Size Size { get; set; }

        public NodifyGroup(NodeGroup nodeGroup)
        {
            NodeGroup = nodeGroup;
            Name = nodeGroup.Name;
            Position = new Point(nodeGroup.Location.X, nodeGroup.Location.Y);
            Size = new Size(nodeGroup.Size.Width, nodeGroup.Size.Height);
        }
    }

    public class NodifyPendingConnection
    {
        private readonly EditorViewModel _editor;
        private NodifyConnector? _source;

        public NodifyPendingConnection(EditorViewModel editor)
        {
            _editor = editor;
            StartCommand = new DelegateCommand<NodifyConnector>(source => _source = source);
            FinishCommand = new DelegateCommand<NodifyConnector>(target =>
            {
                if (target == null || _source == null)
                    return;

                if (_source == target)
                {
                    _editor.TriggerInvalidConnection("Can't connect a connector to itself.");
                    return;
                }

                if (_source.TaskConnector.Type != target.TaskConnector.Type)
                {
                    _editor.TriggerInvalidConnection("Can't connect two connectors of different types.");
                    return;
                }

                if (_source.TaskConnector.Direction == target.TaskConnector.Direction)
                {
                    _editor.TriggerInvalidConnection("Can't connect two output or input connectors.");
                    return;
                }

                // If target is output, swap source and target
                if (target.TaskConnector.Direction == EnumTaskConnectorDirection.Out)
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

    public class WorkflowWrapper
    {
        public WorkflowNode WorkflowNode { get; set; }
        public ObservableCollection<INode> Nodes { get; set; } = [];
        public ObservableCollection<NodifyConnection> Connections { get; set; } = [];

        public WorkflowWrapper(WorkflowNode workflowNode)
        {
            WorkflowNode = workflowNode;

            Dictionary<Guid, NodifyConnector> connectors = new Dictionary<Guid, NodifyConnector>();
            foreach (var group in workflowNode.Groups)
            {
                Nodes.Add(new NodifyGroup(group));
            }

            // Set nodes and connectors
            foreach (var task in workflowNode.Tasks)
            {
                var node = new NodifyNode(workflowNode.Relations[task.Id], task);

                // Store connectors for connections
                foreach (var connector in node.Inputs)
                    connectors.Add(connector.TaskConnector.Id, connector);
                foreach (var connector in node.Outputs)
                    connectors.Add(connector.TaskConnector.Id, connector);

                Nodes.Add(node);
            }

            // Set connections with corresponding connectors
            foreach (var connection in workflowNode.Connections)
            {
                var source = connectors[connection.SourceId];
                var target = connectors[connection.TargetId];
                Connections.Add(new NodifyConnection(source, target));
            }
        }
    }
}
