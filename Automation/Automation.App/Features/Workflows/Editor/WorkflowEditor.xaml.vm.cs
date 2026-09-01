using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Windows;
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
        /// Execution of the workflow started from the editor, null while nothing is running. The
        /// graph can't be edited while it is set : what runs has to stay what is displayed.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsEditable), nameof(IsRunning))]
        [NotifyCanExecuteChangedFor(nameof(StartCommand), nameof(CancelCommand), nameof(AddCommand),
            nameof(RemoveCommand), nameof(OpenSettingsCommand))]
        private TaskInstance? _runningInstance;

        /// <summary>
        /// Whether the graph can be modified, false while an execution is running.
        /// </summary>
        public bool IsEditable => RunningInstance == null;

        public bool IsRunning => RunningInstance != null;

        /// <summary>
        /// Viewport of the editor, used to add the new nodes where the user is actually looking.
        /// </summary>
        [ObservableProperty] private Point _viewportLocation;
        [ObservableProperty] private Size _viewportSize;

        private readonly IExecutionService _execution = SpineViewModel.Instance.Execution;
        private readonly IHistoryService _historyService = SpineViewModel.Instance.History;
        private readonly IToastService _toasts = SpineViewModel.Instance.Toasts;

        public WorkflowEditorViewModel(AutomationWorkflow workflow, IAsyncRelayCommand saveCommand)
        {
            Workflow = workflow;
            SaveCommand = saveCommand;

            Load();
            SelectedNodes.CollectionChanged += (_, _) =>
            {
                RemoveCommand.NotifyCanExecuteChanged();
                OpenSettingsCommand.NotifyCanExecuteChanged();
            };
        }

        /// <summary>
        /// Wrap the graph elements, the connections being resolved to the connectors they link.
        /// </summary>
        private void Load()
        {
            // The graph comes deserialized : its connectors and connections only know each other by
            // id until it is refreshed, and walking it (to build the context samples of a task) reads
            // the references.
            Graph.Refresh();

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
            BaseGraphTask graphTask = task switch
            {
                AutomationWorkflow workflow => new GraphWorkflow(workflow),
                AutomationControl control => new GraphControl(control),
                AutomationTask automationTask => new GraphTask(automationTask),
                _ => throw new NotSupportedException($"Unknown task type '{task.GetType().Name}'")
            };

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
        /// Open the settings of a node : the JSON parameters it runs with, or the input / output of
        /// the workflow for the control tasks. Without a node it falls back on the selected one, the
        /// command being shared by the double click on a node and the editor toolbar.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanOpenSettings))]
        private async Task OpenSettings(NodeViewModel? node)
        {
            node ??= SelectedNodes.FirstOrDefault();
            if (node == null)
                return;

            IReversibleAction? edition = await TaskSettingsViewModel.ShowAsync(node.Model, Workflow);
            if (edition != null)
                History.Apply(edition);
        }

        private bool CanOpenSettings(NodeViewModel? node) => IsEditable && (node != null || SelectedNodes.Count == 1);

        /// <summary>
        /// Remove the selected nodes, along with the connections linked to them.
        /// </summary>
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
                nodes.Count == 1 ? $"Remove '{nodes[0].Name}'" : $"Remove {nodes.Count} nodes",
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
        /// Location of every node when a drag started, so the move can be recorded as a single
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
                moves.Count == 1 ? $"Move '{moves[0].Node.Name}'" : $"Move {moves.Count} nodes",
                () =>
                {
                    foreach ((NodeViewModel node, _, Point to) in moves)
                        node.Location = to;
                },
                () =>
                {
                    foreach ((NodeViewModel node, Point from, _) in moves)
                        node.Location = from;
                }));
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
            if (!Graph.CanConnect(source.Model, target.Model))
                return;

            var connection = new ConnectionViewModel(source, target);
            History.Apply(new ReversibleAction(
                $"Connect '{source.Node.Name}' to '{target.Node.Name}'",
                () => AddConnection(connection),
                () => RemoveConnection(connection)));
        }

        #region Execution

        /// <summary>
        /// Start the workflow as it is currently saved, the graph turning read only until the
        /// execution is over. Returns as soon as the execution started, its end being reported by
        /// the history service.
        /// </summary>
        [RelayCommand(CanExecute = nameof(IsEditable))]
        private async Task Start()
        {
            _historyService.InstanceUpdated += OnInstanceUpdated;
            try
            {
                RunningInstance = await _execution.StartAsync(Workflow);
            }
            catch (Exception exception)
            {
                Stop();
                _toasts.Error(exception.Message, $"The workflow '{Workflow.Metadata.Name}' could not be started");
                return;
            }

            // The execution may already be over by the time it is awaited, its end then having been
            // reported before there was anything to match it against.
            if ((RunningInstance.State & EnumTaskState.Finished) != 0)
                Stop();
        }

        /// <summary>
        /// Cancel the running execution. The graph only becomes editable again once the execution
        /// actually reports itself as finished.
        /// </summary>
        [RelayCommand(CanExecute = nameof(IsRunning))]
        private async Task Cancel()
        {
            TaskInstance? instance = RunningInstance;
            if (instance == null)
                return;

            try
            {
                await _execution.CancelAsync(instance.Id);
            }
            catch (Exception exception)
            {
                _toasts.Error(exception.Message, $"The workflow '{Workflow.Metadata.Name}' could not be canceled");
            }
        }

        private void OnInstanceUpdated(TaskInstance instance)
        {
            // The instances are reported by the threads running the executions.
            Dispatch(() =>
            {
                if (RunningInstance?.Id == instance.Id && (instance.State & EnumTaskState.Finished) != 0)
                    Stop();
            });
        }

        /// <summary>
        /// Stop following the execution, the graph becoming editable again.
        /// </summary>
        private void Stop()
        {
            _historyService.InstanceUpdated -= OnInstanceUpdated;
            RunningInstance = null;
        }

        /// <summary>
        /// The undo / redo also modifies the graph, so it follows whether the editor is editable.
        /// </summary>
        partial void OnRunningInstanceChanged(TaskInstance? value) => History.IsEnabled = IsEditable;

        private static void Dispatch(Action action)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
                action();
            else
                dispatcher.Invoke(action);
        }

        #endregion

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
