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
            nameof(RemoveCommand), nameof(OpenSettingsCommand), nameof(RenameCommand))]
        private TaskInstance? _runningInstance;

        /// <summary>
        /// Whether the graph can be modified, false while an execution is running.
        /// </summary>
        public bool IsEditable => RunningInstance == null;

        public bool IsRunning => RunningInstance != null;

        /// <summary>
        /// How far the run being displayed got, null while no run is displayed. Counted on the nodes
        /// it went through rather than on the graph : a graph is never walked whole, its branches
        /// leaving nodes out, so there is no total to progress towards.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasProgress))]
        private string? _progress;

        public bool HasProgress => !string.IsNullOrEmpty(Progress);

        /// <summary>
        /// Viewport of the editor, used to add the new nodes where the user is actually looking.
        /// </summary>
        [ObservableProperty] private Point _viewportLocation;
        [ObservableProperty] private Size _viewportSize;

        private readonly IScopedService _scoped = SpineViewModel.Instance.Scoped;
        private readonly IExecutionService _execution = SpineViewModel.Instance.Execution;
        private readonly IHistoryService _historyService = SpineViewModel.Instance.History;
        private readonly IToastService _toasts = SpineViewModel.Instance.Toasts;

        /// <summary>
        /// What the graph would run into, as the last refresh found it. Held rather than rebuilt by
        /// whoever needs it : two previews of the same graph are two walks of it, and they can
        /// disagree — the global context lands asynchronously, so one built before it arrives
        /// resolves "$global" against nothing.
        /// </summary>
        public GraphExecutionPreview? Preview { get; private set; }

        /// <summary>
        /// The context of the scopes holding the workflow, which a mapping reads as "$global".
        /// </summary>
        private JToken? _globalContext;

        /// <summary>
        /// The tasks the nodes of the graph point at, kept from the load : refreshing the graph
        /// needs them, a node handing over nothing of its own being only known through them.
        /// </summary>
        private Dictionary<Guid, BaseAutomationTask> _tasks = [];

        public WorkflowEditorViewModel(AutomationWorkflow workflow, IAsyncRelayCommand saveCommand)
        {
            Workflow = workflow;
            SaveCommand = saveCommand;

            _ = LoadAsync();
            SelectedNodes.CollectionChanged += (_, _) =>
            {
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

            try
            {
                _globalContext = await _scoped.GetContextAsync(Workflow.Id);
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

            _tasks = tasks;
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

        /// <summary>
        /// What the graph would run into : the nodes whose mapping cannot resolve what they read,
        /// and the branches they cannot resolve it on. Read from the graph rather than from a run,
        /// so it shows while the workflow is being drawn.
        /// </summary>
        private void RefreshPreview()
        {
            Preview = null;

            try
            {
                // Re-wired first : a node added since the last load holds connectors nothing linked
                // to it yet, and walking the graph reads the nodes a connection leads to rather than
                // the ids it holds.
                Graph.Refresh(_tasks, force: true);

                GraphContextResolution resolution = new() { GlobalContext = _globalContext };
                GraphExecutionPreview preview = new();
                preview.BuildSamples(Graph, resolution);

                Preview = preview;
            }
            catch
            {
                // A graph that can't be walked at all says nothing about its nodes : showing no
                // error is better than showing one on every one of them.
            }

            foreach (NodeViewModel node in Nodes)
                node.Errors = Messages(Preview?.NodesErrors, node.Model.Id);

            foreach (ConnectionViewModel connection in Connections)
                connection.Errors = Messages(Preview?.EdgesErrors, connection.Model.Edge, named: true);
        }

        /// <summary>
        /// What [errors] holds against [key], the duplicates two branches carrying the same thing
        /// produce left out. Named when the reader needs to know which node holds them : an edge
        /// stands for what the node it leads to cannot handle, not for something of its own.
        /// </summary>
        private IReadOnlyList<string> Messages<TKey>(
            Dictionary<TKey, List<GraphPreviewError>>? errors,
            TKey key,
            bool named = false)
            where TKey : notnull
        {
            if (errors == null || !errors.TryGetValue(key, out List<GraphPreviewError>? found))
                return [];

            return
            [
                .. found
                    .Select(x => named ? $"{NameOf(x.NodeId)} : {x.Message}" : x.Message)
                    .Distinct()
            ];
        }

        private string NameOf(Guid nodeId) => Nodes.FirstOrDefault(x => x.Model.Id == nodeId)?.Name ?? "?";

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
        [RelayCommand(CanExecute = nameof(CanOpenSettings))]
        private async Task OpenSettings(NodeViewModel? node)
        {
            node ??= SelectedNodes.FirstOrDefault();
            if (node == null)
                return;

            IReversibleAction? edition = await TaskSettingsViewModel.ShowAsync(node.Model, Preview);
            if (edition != null)
                History.Apply(edition);
        }

        private bool CanOpenSettings(NodeViewModel? node) => IsEditable && (node != null || SelectedNodes.Count == 1);

        #region Renaming
        /// <summary>
        /// Start renaming [node] on the graph : its label becomes a box holding the name it has, and
        /// nothing is written to the graph until what was typed is committed.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanRename))]
        private void Rename(NodeViewModel? node)
        {
            node ??= SelectedNodes.FirstOrDefault();
            if (node == null)
                return;

            node.NameDraft = node.Name;
            node.IsRenaming = true;
        }

        private bool CanRename(NodeViewModel? node) => IsEditable && (node != null || SelectedNodes.Count == 1);

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

        /// <summary>
        /// Leave the node named the way it was, whatever was typed.
        /// </summary>
        [RelayCommand]
        private void CancelRename(NodeViewModel? node)
        {
            if (node != null)
                node.IsRenaming = false;
        }
        #endregion

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

            var existingConnection = GetConnectionsBetween(source.Model, target.Model);
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

        #region Execution

        /// <summary>
        /// Instances reported between the subscription and the start handing over the instance of
        /// the run : the executor walks the graph on a thread of its own and the first nodes are
        /// usually reported by then, with nothing yet to match them against.
        /// </summary>
        private readonly List<TaskInstance> _reportedBeforeStart = [];

        /// <summary>
        /// Whether a start is being awaited, which is the only time a report is worth holding onto :
        /// what is reported once the run is over belongs to nothing the editor follows.
        /// </summary>
        private bool _isStarting;

        /// <summary>
        /// Start the workflow, the graph turning read only until the execution is over. Returns as
        /// soon as the execution started, its end being reported by the history service.
        /// <para>
        /// A workflow expecting an input asks for it first, the run being cancelled when the user
        /// gives up on the settings. The graph is then saved : what runs is the persisted workflow,
        /// so what is displayed has to be what was persisted.
        /// </para>
        /// </summary>
        [RelayCommand(CanExecute = nameof(IsEditable))]
        private async Task Start()
        {
            JToken? settings = null;
            if (StartSettingsViewModel.IsExpectingSettings(Workflow))
            {
                settings = await StartSettingsViewModel.ShowAsync(Workflow);
                if (settings == null)
                    return;
            }

            if (!await SaveAsync())
                return;

            // What is displayed of a run belongs to it : the previous one is cleared rather than
            // left over the graph of the new one.
            ResetProgress();

            // Added and updated both : a node is reported as it starts, then again as it ends.
            _isStarting = true;
            _historyService.InstanceAdded += OnInstanceReported;
            _historyService.InstanceUpdated += OnInstanceReported;
            try
            {
                // Started by id : what runs is the workflow just saved, read back by the executor.
                RunningInstance = await _execution.StartAsync(Workflow.Id, settings);
            }
            catch (Exception exception)
            {
                Stop();
                _toasts.Error(exception.Message, $"The workflow '{Workflow.Metadata.Name}' could not be started");
                return;
            }
            finally
            {
                _isStarting = false;
            }

            // What the run reported while its start was being awaited, now that the instance it all
            // hangs under is known.
            List<TaskInstance> reported = [.. _reportedBeforeStart];
            _reportedBeforeStart.Clear();
            foreach (TaskInstance instance in reported)
                Apply(instance);

            // The execution may already be over by the time it is awaited, its end then having been
            // reported before there was anything to match it against.
            if (RunningInstance is TaskInstance running && (running.State & EnumTaskState.Finished) != 0)
            {
                Stop();
                Report(running);
            }
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

        /// <summary>
        /// Clear what a run left on the graph : the states of the nodes, the path it took and how
        /// far it got.
        /// </summary>
        private void ResetProgress()
        {
            foreach (NodeViewModel node in Nodes)
                node.Follow();

            foreach (ConnectionViewModel connection in Connections)
                connection.IsTraversed = false;

            _reportedBeforeStart.Clear();
            Progress = null;
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

        /// <summary>
        /// An instance changed while a run is being followed, reported by the thread executing it.
        /// </summary>
        private void OnInstanceReported(TaskInstance instance)
        {
            Dispatch(() =>
            {
                // Reported before the start handed over the instance of the run : held onto rather
                // than dropped, it is the beginning of the very run being started.
                if (RunningInstance == null)
                {
                    if (_isStarting)
                        _reportedBeforeStart.Add(instance);
                    return;
                }

                Apply(instance);
            });
        }

        /// <summary>
        /// Display what [instance] tells of the run being followed : the workflow itself, which ends
        /// the run, or one of its nodes, whose progress is drawn on the graph.
        /// </summary>
        private void Apply(TaskInstance instance)
        {
            TaskInstance? running = RunningInstance;
            if (running == null)
                return;

            if (running.Id == instance.Id)
            {
                if ((instance.State & EnumTaskState.Finished) != 0)
                {
                    Stop();
                    Report(instance);
                }
                return;
            }

            // Only the nodes of this very run, a workflow can be running in more than one place
            // (its own editor, a node of another graph, a schedule).
            if (running.Id != instance.ParentInstanceId || instance.NodeId is not Guid nodeId)
                return;

            NodeViewModel? node = Nodes.FirstOrDefault(x => x.Model.Id == nodeId);
            if (node == null)
                return;

            // The instance is only timed once it is over, what it holds meanwhile being the last
            // change of state rather than an end.
            bool finished = (instance.State & EnumTaskState.Finished) != 0;
            node.Follow(
                instance.State,
                finished && instance.FinishedAt is DateTime end ? end - instance.CreatedAt : null,
                instance.State == EnumTaskState.Failed ? FirstLine(instance.Output?.ToString()) : null);

            Traverse(instance.Previous?.NodeId, nodeId);
            RefreshProgress();
        }

        /// <summary>
        /// Draw the branch the run reached [nodeId] through. Only the nodes it links are known, so
        /// every connection between the two is drawn : as far as the instance says, that is how the
        /// run came in.
        /// </summary>
        private void Traverse(Guid? previousNodeId, Guid nodeId)
        {
            if (previousNodeId is not Guid previous)
                return;

            foreach (ConnectionViewModel connection in Connections)
            {
                if (connection.Source.Node.Model.Id == previous && connection.Target.Node.Model.Id == nodeId)
                    connection.IsTraversed = true;
            }
        }

        /// <summary>
        /// How far the run got, read from the nodes it went through : what it finished, and what it
        /// still has running.
        /// </summary>
        private void RefreshProgress()
        {
            int finished = 0;
            int running = 0;
            foreach (NodeViewModel node in Nodes)
            {
                if (node.State is not EnumTaskState state)
                    continue;

                if ((state & EnumTaskState.Finished) != 0)
                    finished++;
                else
                    running++;
            }

            Progress = finished == 0 && running == 0
                ? null
                : running == 0 ? $"{finished} done" : $"{finished} done, {running} running";
        }

        /// <summary>
        /// Tell how the run ended, the editor being the place it was started from.
        /// </summary>
        private void Report(TaskInstance instance)
        {
            string name = Workflow.Metadata.Name;
            switch (instance.State)
            {
                case EnumTaskState.Completed:
                    _toasts.Success($"The workflow '{name}' has been executed.", "Workflow completed");
                    break;
                case EnumTaskState.Canceled:
                    _toasts.Warning($"The workflow '{name}' has been canceled.", "Workflow canceled");
                    break;
                default:
                    // The failure of a task is stored as its stack trace, only its first line is
                    // worth a toast : the history holds the rest.
                    _toasts.Error(FirstLine(instance.Output?.ToString()) ?? "The execution failed.", $"Workflow '{name}' failed");
                    break;
            }
        }

        private static string? FirstLine(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            string line = text.ReplaceLineEndings("\n").Split('\n')[0].Trim();
            return string.IsNullOrEmpty(line) ? null : line;
        }

        /// <summary>
        /// Stop following the execution, the graph becoming editable again. The states left on the
        /// nodes are kept : they are what the run amounted to.
        /// </summary>
        private void Stop()
        {
            _historyService.InstanceAdded -= OnInstanceReported;
            _historyService.InstanceUpdated -= OnInstanceReported;
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


        /// <summary>
        /// Get the connection between the [source] and [target] if it exist.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private ConnectionViewModel? GetConnectionsBetween(GraphConnector source, GraphConnector target)
        {
            return Connections.FirstOrDefault(x => x.Model.SourceId == source.Id && x.Model.TargetId == target.Id);
        }

        #endregion
    }
}
