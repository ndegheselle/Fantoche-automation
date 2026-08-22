using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Automation.App.Features.Workflows.Controls;
using Automation.App.Features.Workflows.Editor.History;
using Automation.App.Features.Workflows.Editor.ViewModels;
using Automation.Shared.Data.Graph;
using Automation.Shared.Data.Scoped;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Automation.App.Features.Workflows.Editor
{
    /// <summary>
    /// The graph of a workflow as edited by Nodify : the graph elements are wrapped into view
    /// models, every modification going through the <see cref="History"/> so that it can be undone
    /// and so that we know if the workflow has to be saved.
    /// </summary>
    public partial class WorkflowEditorViewModel : ObservableObject
    {
        public AutomationWorkflow Workflow { get; }

        public TasksGraph Graph => Workflow.Graph;

        public ObservableCollection<NodeViewModel> Nodes { get; } = [];

        public ObservableCollection<ConnectionViewModel> Connections { get; } = [];

        /// <summary>
        /// Selection of the editor, filled by Nodify.
        /// </summary>
        public ObservableCollection<NodeViewModel> SelectedNodes { get; } = [];

        public EditorHistory History { get; } = new();

        /// <summary>
        /// Save of the graph, handled by the page owning the editor. It is only enabled while the
        /// <see cref="History"/> has unsaved changes, the general infos having their own save.
        /// </summary>
        public IAsyncRelayCommand SaveCommand { get; }

        /// <summary>
        /// Viewport of the editor, used to add the new nodes where the user is actually looking.
        /// </summary>
        [ObservableProperty] private Point _viewportLocation;
        [ObservableProperty] private Size _viewportSize;

        public WorkflowEditorViewModel(AutomationWorkflow workflow, IAsyncRelayCommand saveCommand)
        {
            Workflow = workflow;
            SaveCommand = saveCommand;

            Load();
            SelectedNodes.CollectionChanged += (_, _) => RemoveCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Wrap the graph elements, the connections being resolved to the connectors they link.
        /// </summary>
        private void Load()
        {
            var connectors = new Dictionary<Guid, ConnectorViewModel>();
            foreach (BaseGraphTask task in Graph.Nodes.OfType<BaseGraphTask>())
            {
                var node = new NodeViewModel(task);
                Nodes.Add(node);
                foreach (ConnectorViewModel connector in node.Connectors)
                    connectors[connector.Model.Id] = connector;
            }

            foreach (GraphConnection connection in Graph.Connections)
            {
                if (connectors.TryGetValue(connection.SourceId, out ConnectorViewModel? source)
                    && connectors.TryGetValue(connection.TargetId, out ConnectorViewModel? target))
                    Connections.Add(new ConnectionViewModel(connection, source, target));
            }
        }

        /// <summary>
        /// Pick an existing task or workflow and add it to the graph. The workflow being edited is
        /// left out of the selection, it can't contain itself.
        /// </summary>
        [RelayCommand]
        private async Task Add()
        {
            BaseAutomationTask? task = await TaskSelectionViewModel.ShowAsync(Workflow.Id);
            if (task == null)
                return;
            Add(task);
        }

        /// <summary>
        /// Add a node targeting [task] at the center of the viewport.
        /// </summary>
        public void Add(BaseAutomationTask task)
        {
            BaseGraphTask graphTask = task switch
            {
                AutomationWorkflow workflow => new GraphWorkflow(workflow),
                AutomationControl control => new GraphControl(control),
                AutomationTask automationTask => new GraphTask(automationTask),
                _ => throw new NotSupportedException($"Unknown task type '{task.GetType().Name}'")
            };

            // The name is only a label within the graph, it has to stay unique to identify the node
            graphTask.Metadata.Name = Graph.GetUniqueNodeName(graphTask.Metadata.Name);
            graphTask.LocationX = ViewportLocation.X + ViewportSize.Width / 2;
            graphTask.LocationY = ViewportLocation.Y + ViewportSize.Height / 2;
            graphTask.AutomationTask = task;

            var node = new NodeViewModel(graphTask);
            History.Apply(new ReversibleAction($"Add '{node.Name}'", () => AddNode(node), () => RemoveNode(node)));
        }

        /// <summary>
        /// Remove the selected nodes, along with the connections linked to them.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanRemove))]
        private void Remove()
        {
            List<NodeViewModel> nodes = [.. SelectedNodes];
            List<ConnectionViewModel> connections =
                [.. Connections.Where(x => nodes.Contains(x.Source.Node) || nodes.Contains(x.Target.Node))];

            History.Apply(new ReversibleAction(
                nodes.Count == 1 ? $"Remove '{nodes[0].Name}'" : $"Remove {nodes.Count} nodes",
                () =>
                {
                    foreach (ConnectionViewModel connection in connections)
                        RemoveConnection(connection);
                    foreach (NodeViewModel node in nodes)
                        RemoveNode(node);
                },
                () =>
                {
                    foreach (NodeViewModel node in nodes)
                        AddNode(node);
                    foreach (ConnectionViewModel connection in connections)
                        AddConnection(connection);
                }));
        }

        private bool CanRemove => SelectedNodes.Count > 0;

        /// <summary>
        /// Location of every selected node when a drag started, so the move can be recorded as a
        /// single reversible action once it completes.
        /// </summary>
        private readonly Dictionary<NodeViewModel, Point> _dragOrigins = [];

        [RelayCommand]
        private void ItemsDragStarted()
        {
            _dragOrigins.Clear();
            foreach (NodeViewModel node in SelectedNodes)
                _dragOrigins[node] = node.Location;
        }

        [RelayCommand]
        private void ItemsDragCompleted()
        {
            Dictionary<NodeViewModel, Point> origins = _dragOrigins
                .Where(x => x.Key.Location != x.Value)
                .ToDictionary(x => x.Key, x => x.Value);
            _dragOrigins.Clear();

            if (origins.Count == 0)
                return;

            Dictionary<NodeViewModel, Point> destinations = origins.ToDictionary(x => x.Key, x => x.Key.Location);

            History.Apply(new ReversibleAction(
                origins.Count == 1 ? $"Move '{origins.Keys.First().Name}'" : $"Move {origins.Count} nodes",
                () =>
                {
                    foreach ((NodeViewModel node, Point location) in destinations)
                        node.Location = location;
                },
                () =>
                {
                    foreach ((NodeViewModel node, Point location) in origins)
                        node.Location = location;
                }));
        }

        /// <summary>
        /// Complete a pending connection dragged from one connector to another : the parameter is a
        /// tuple of the connector the drag started from and the one it was dropped on. A connection
        /// only makes sense between an output and an input of two different nodes.
        /// </summary>
        [RelayCommand]
        private void ConnectionCompleted(object? parameter)
        {
            if (parameter is not ITuple { Length: 2 } pending)
                return;
            if (pending[0] is not ConnectorViewModel first || pending[1] is not ConnectorViewModel second)
                return;

            bool firstIsOutput = first.Node.Outputs.Contains(first);
            bool secondIsOutput = second.Node.Outputs.Contains(second);
            if (first.Node == second.Node || firstIsOutput == secondIsOutput)
                return;

            ConnectorViewModel source = firstIsOutput ? first : second;
            ConnectorViewModel target = firstIsOutput ? second : first;
            if (Connections.Any(x => x.Source == source && x.Target == target))
                return;

            var connection = new ConnectionViewModel(source, target);
            History.Apply(new ReversibleAction(
                $"Connect '{source.Node.Name}' to '{target.Node.Name}'",
                () => AddConnection(connection),
                () => RemoveConnection(connection)));
        }

        #region Graph edition

        private void AddNode(NodeViewModel node)
        {
            Graph.Nodes.Add(node.Model);
            Nodes.Add(node);
        }

        private void RemoveNode(NodeViewModel node)
        {
            SelectedNodes.Remove(node);
            Nodes.Remove(node);
            Graph.Nodes.Remove(node.Model);
        }

        private void AddConnection(ConnectionViewModel connection)
        {
            Graph.Connections.Add(connection.Model);
            Connections.Add(connection);
            connection.Source.IsConnected = true;
            connection.Target.IsConnected = true;
        }

        private void RemoveConnection(ConnectionViewModel connection)
        {
            Graph.Connections.Remove(connection.Model);
            Connections.Remove(connection);
            connection.Source.IsConnected = IsConnected(connection.Source);
            connection.Target.IsConnected = IsConnected(connection.Target);
        }

        private bool IsConnected(ConnectorViewModel connector)
            => Connections.Any(x => x.Source == connector || x.Target == connector);

        #endregion
    }
}
