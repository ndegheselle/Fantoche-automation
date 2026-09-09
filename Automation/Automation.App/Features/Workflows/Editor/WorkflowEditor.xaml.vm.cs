using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Automation.App.Common;
using Automation.App.Features.Workflows.Controls;
using Automation.App.Features.Workflows.Editor.History;
using Automation.App.Features.Workflows.Editor.ViewModels;
using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Graph;
using Automation.Shared.Data.Scoped;
using Automation.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Joufflu.Feedback;
using Newtonsoft.Json.Linq;

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

        /// <summary>Selection of the editor, filled by Nodify.</summary>
        public ObservableCollection<NodeViewModel> SelectedNodes { get; } = [];

        public EditorHistory History { get; } = new();

        /// <summary>
        /// Save of the graph, handled by the page owning the editor. It is only enabled while the
        /// <see cref="History"/> has unsaved changes, the general infos having their own save.
        /// </summary>
        public IAsyncRelayCommand SaveCommand { get; }

        /// <summary>
        /// The run started from the editor and followed on its graph. The graph can't be edited
        /// while one is on : what runs has to stay what is displayed.
        /// </summary>
        public WorkflowRunViewModel Run { get; }

        /// <summary>Whether the graph can be modified, false while an execution is running.</summary>
        public bool IsEditable => !Run.IsRunning;

        /// <summary>Viewport of the editor, so a new node lands where the user is looking.</summary>
        [ObservableProperty] private Point _viewportLocation;
        [ObservableProperty] private Size _viewportSize;

        private readonly IScopedService _scoped;
        private readonly IToastService _toasts;

        private readonly GraphPreviewer _previewer = new();

        /// <summary>What the graph would run into, as the last refresh found it.</summary>
        public GraphExecutionPreview? Preview => _previewer.Preview;

        public WorkflowEditorViewModel(AutomationWorkflow workflow, IAsyncRelayCommand saveCommand, AppServices services)
        {
            Workflow = workflow;
            SaveCommand = saveCommand;
            _scoped = services.Scoped;
            _toasts = services.Toasts;
            Run = new WorkflowRunViewModel(workflow, Nodes, Connections, SaveAsync, services);

            _ = LoadAsync();
            SelectedNodes.CollectionChanged += (_, _) =>
            {
                RemoveCommand.NotifyCanExecuteChanged();
                OpenSettingsCommand.NotifyCanExecuteChanged();
                RenameCommand.NotifyCanExecuteChanged();
            };

            // A run turning the graph read only is a change of the editor as much as of the run :
            // the undo / redo is a modification like any other, so it follows too.
            Run.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName != nameof(WorkflowRunViewModel.IsRunning))
                    return;

                OnPropertyChanged(nameof(IsEditable));
                History.IsEnabled = IsEditable;
                AddCommand.NotifyCanExecuteChanged();
                RemoveCommand.NotifyCanExecuteChanged();
                OpenSettingsCommand.NotifyCanExecuteChanged();
                RenameCommand.NotifyCanExecuteChanged();
            };

            // Refreshed from the history rather than from each command : an undo and a redo change
            // the graph as much as the edit they replay, and only the history knows of them.
            History.ExecutionChanged += RefreshPreview;
        }

        /// <summary>
        /// Wrap the graph elements, the connections being resolved to the connectors they link. The
        /// tasks the nodes point at are loaded along : a node handing over nothing of its own is
        /// only known through them, and the preview walks the graph reading what they declare.
        /// </summary>
        private async Task LoadAsync()
        {
            Dictionary<Guid, BaseAutomationTask> tasks = [];
            JToken? globalContext = null;

            try
            {
                globalContext = await _scoped.GetContextAsync(Workflow.Id);
            }
            catch
            {
                // Without it a mapping referencing the global context simply can't be resolved, and
                // the preview says so like it says the rest.
            }

            try
            {
                List<Guid> taskIds = await _scoped.GetGraphTaskIdsAsync(Workflow.Id);
                foreach (ScopedElement element in await _scoped.GetAsync(taskIds))
                {
                    if (element is BaseAutomationTask task)
                        tasks[task.Id] = task;
                }
            }
            catch (Exception exception)
            {
                // The graph is still displayed without them, only the validation of the parameters
                // has less to say.
                _toasts.Error(exception.Message, $"The tasks of '{Workflow.Metadata.Name}' could not be loaded");
            }

            _previewer.Load(globalContext, tasks);
            Graph.Refresh(tasks, force: true);

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

            RefreshPreview();
        }

        private void RefreshPreview() => _previewer.Refresh(Graph, Nodes, Connections);

        /// <summary>
        /// Pick an existing task or workflow and add it to the graph. The workflow being edited is
        /// left out of the selection, it can't contain itself.
        /// </summary>
        [RelayCommand(CanExecute = nameof(IsEditable))]
        private async Task Add()
        {
            BaseAutomationTask? task = await TaskSelectionViewModel.ShowAsync(Workflow.Id);
            if (task == null)
                return;
            Add(task);
        }

        /// <summary>
        /// Add a node targeting [task] at [location] in the graph, at the center of the viewport
        /// when no location is given.
        /// </summary>
        public void Add(BaseAutomationTask task, Point? location = null)
        {
            BaseGraphTask graphTask = BaseGraphTask.For(task);

            // A workflow is entered once and left once : the second start or end never makes it in.
            if (!Graph.CanAdd(graphTask))
            {
                _toasts.Warning(
                    $"The workflow '{Workflow.Metadata.Name}' already holds one.",
                    $"'{task.Metadata.Name}' can only be added once");
                return;
            }

            // The name is only a label within the graph, it has to stay unique to identify the node
            graphTask.Metadata.Name = Graph.GetUniqueNodeName(graphTask.Metadata.Name);
            Point placement = location ?? new Point(
                ViewportLocation.X + ViewportSize.Width / 2,
                ViewportLocation.Y + ViewportSize.Height / 2);
            graphTask.LocationX = placement.X;
            graphTask.LocationY = placement.Y;
            graphTask.AutomationTask = task;

            var node = new NodeViewModel(graphTask);
            History.Apply(new ReversibleAction($"Add '{node.Name}'", () => AddNode(node), () => RemoveNode(node)));
        }

        /// <summary>
        /// Open the settings of a node : the mapping it runs with, whatever its kind. Without a node
        /// it falls back on the selected one, the command being shared by the double click on a node
        /// and the editor toolbar.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanActOn))]
        private async Task OpenSettings(NodeViewModel? node)
        {
            node = Target(node);
            if (node == null)
                return;

            IReversibleAction? edition = await TaskSettingsViewModel.ShowAsync(node.Model, Preview);
            if (edition != null)
                History.Apply(edition);
        }

        /// <summary>
        /// Whether [node], or the selection it falls back on, can be acted upon : one node has to be
        /// named, and the graph has to be editable.
        /// </summary>
        private bool CanActOn(NodeViewModel? node) => IsEditable && (node != null || SelectedNodes.Count == 1);

        /// <summary>
        /// [node], falling back on the selected one : the commands are shared by the toolbar, which
        /// hands over nothing, and the graph itself, which hands over the node acted on.
        /// </summary>
        private NodeViewModel? Target(NodeViewModel? node) => node ?? SelectedNodes.FirstOrDefault();

        #region Renaming
        /// <summary>
        /// Start renaming [node] on the graph : its label becomes a box holding the name it has, and
        /// nothing is written to the graph until what was typed is committed.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanActOn))]
        private void Rename(NodeViewModel? node)
        {
            node = Target(node);
            if (node == null)
                return;

            node.NameDraft = node.Name;
            node.IsRenaming = true;
        }

        /// <summary>
        /// Apply what was typed, unless it cannot name the node : a node has to be named, and a name
        /// has to tell it apart from the others. A node several branches lead into is read by name,
        /// so two nodes sharing one leaves a join unable to say which of them it reads — the graph
        /// then resolves to nothing at all rather than to something wrong.
        /// </summary>
        [RelayCommand]
        private void CommitRename(NodeViewModel? node)
        {
            // Committing closes the box, whose losing the focus commits again : whichever comes
            // second has nothing left to do.
            if (node == null || !node.IsRenaming)
                return;

            node.IsRenaming = false;

            string name = node.NameDraft.Trim();
            if (name == node.Name)
                return;

            if (string.IsNullOrEmpty(name))
            {
                _toasts.Error("A node has to be named.", $"'{node.Name}' was not renamed");
                return;
            }

            if (Graph.Nodes.Any(x => x != node.Model && x.Name == name))
            {
                _toasts.Error($"'{name}' is already the name of another node.", $"'{node.Name}' was not renamed");
                return;
            }

            string previous = node.Name;
            History.Apply(new ReversibleAction(
                $"Rename '{previous}' to '{name}'",
                () => node.Model.Metadata.Name = name,
                () => node.Model.Metadata.Name = previous));
        }

        /// <summary>Leave the node named the way it was, whatever was typed.</summary>
        [RelayCommand]
        private void CancelRename(NodeViewModel? node)
        {
            if (node != null)
                node.IsRenaming = false;
        }
        #endregion

        /// <summary>Remove the selected nodes, along with the connections linked to them.</summary>
        [RelayCommand(CanExecute = nameof(CanRemove))]
        private void Remove()
        {
            List<NodeViewModel> nodes = [.. SelectedNodes];
            // Materialized right away : executing mutates the very collection it reads.
            List<ConnectionViewModel> connections =
                [.. Connections.Where(x => nodes.Contains(x.Source.Node) || nodes.Contains(x.Target.Node))];

            // Disconnecting comes first, a node only leaving the graph once nothing links to it
            // anymore. The composite reverts its steps backwards, so the nodes come back before their
            // connections without that ordering having to be written a second time.
            History.Apply(new CompositeReversibleAction(
                $"Remove {nodes.Count} node(s)",
                new ReversibleAction(
                    $"Disconnect {connections.Count} connection(s)",
                    () =>
                    {
                        foreach (ConnectionViewModel connection in connections)
                            RemoveConnection(connection);
                    },
                    () =>
                    {
                        foreach (ConnectionViewModel connection in connections)
                            AddConnection(connection);
                    }),
                new ReversibleAction(
                    $"Remove {nodes.Count} node(s)",
                    () =>
                    {
                        foreach (NodeViewModel node in nodes)
                            RemoveNode(node);
                    },
                    () =>
                    {
                        foreach (NodeViewModel node in nodes)
                            AddNode(node);
                    })));
        }

        private bool CanRemove => IsEditable && SelectedNodes.Count > 0;

        /// <summary>
        /// Location of every node when a drag started, so the move is recorded as a single
        /// reversible action once it completes.
        /// </summary>
        private readonly Dictionary<NodeViewModel, Point> _dragOrigins = [];

        [RelayCommand]
        private void ItemsDragStarted()
        {
            // Every node is snapshotted rather than only the selection : Nodify raises this for the
            // selected containers but also when it pushes items around, and the completion keeps
            // whichever nodes actually moved.
            _dragOrigins.Clear();
            foreach (NodeViewModel node in Nodes)
                _dragOrigins[node] = node.Location;
        }

        [RelayCommand]
        private void ItemsDragCompleted()
        {
            // Materialized right away : the destinations have to be read while the nodes are still
            // where the drag left them, an undo moving them back to their origin.
            List<(NodeViewModel Node, Point From, Point To)> moves =
                [.. _dragOrigins.Where(x => x.Key.Location != x.Value).Select(x => (x.Key, x.Value, x.Key.Location))];
            _dragOrigins.Clear();

            if (moves.Count == 0)
                return;

            History.Apply(new ReversibleAction(
                $"Move {moves.Count} node(s)",
                () =>
                {
                    foreach ((NodeViewModel node, _, Point to) in moves)
                        node.Location = to;
                },
                () =>
                {
                    foreach ((NodeViewModel node, Point from, _) in moves)
                        node.Location = from;
                })
            {
                // Where the nodes sit says nothing about what the graph resolves to.
                ChangesExecution = false,
            });
        }

        /// <summary>
        /// Complete a pending connection dragged from one connector to another : the parameter is a
        /// tuple of the connector the drag started from and the one it was dropped on. Whether the
        /// connection is allowed is up to the graph.
        /// </summary>
        [RelayCommand]
        private void ConnectionCompleted(object? parameter)
        {
            // ITuple rather than the concrete type : Nodify packs the pair as a tuple without
            // documenting which kind.
            if (!IsEditable
                || parameter is not ITuple { Length: 2 } pending
                || pending[0] is not ConnectorViewModel first
                || pending[1] is not ConnectorViewModel second)
                return;

            // Dragging an input onto an output makes the same connection as the other way around.
            ConnectorViewModel source = first.IsOutput ? first : second;
            ConnectorViewModel target = first.IsOutput ? second : first;

            var existingConnection = FindConnection(source.Model, target.Model);
            if (existingConnection != null)
            {
                History.Apply(new ReversibleAction(
                    $"Unconnecting '{source.Node.Name}' to '{target.Node.Name}'",
                    () => RemoveConnection(existingConnection),
                    () => AddConnection(existingConnection)));
                return;
            }

            if (!Graph.CanConnect(source.Model, target.Model))
                return;

            var connection = new ConnectionViewModel(source, target);
            History.Apply(new ReversibleAction(
                $"Connect '{source.Node.Name}' to '{target.Node.Name}'",
                () => AddConnection(connection),
                () => RemoveConnection(connection)));
        }

        /// <summary>
        /// Persist the workflow so that what runs is what is displayed, and tell whether it can be
        /// started : a graph that couldn't be written would be followed node by node against a run
        /// of another version of itself.
        /// </summary>
        private async Task<bool> SaveAsync()
        {
            try
            {
                // Written whether or not the graph was edited : the save writes the whole workflow,
                // and what a run reads of it goes beyond the graph (its settings, its schemas).
                await SaveCommand.ExecuteAsync(null);
            }
            catch (Exception exception)
            {
                _toasts.Error(exception.Message, $"The graph of the workflow '{Workflow.Metadata.Name}' could not be saved");
                return false;
            }

            // The history still holding changes means the save didn't go through, whichever way it
            // reported it.
            return !History.HasUnsavedChanges;
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


        /// <summary>The connection linking [source] to [target], null when there is none.</summary>
        private ConnectionViewModel? FindConnection(GraphConnector source, GraphConnector target)
            => Connections.FirstOrDefault(x => x.Model.SourceId == source.Id && x.Model.TargetId == target.Id);

        #endregion
    }
}
