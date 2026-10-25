using Fantoche.App.Features.Workflows.Controls;
using Fantoche.App.Common;
using Fantoche.App.Features.UserInterfaces;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Data.Scoped;
using Fantoche.Shared.Services;
using Fantoche.Worker.UserInterfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Joufflu.Feedback;
using Newtonsoft.Json.Linq;

namespace Fantoche.App.Features.Workflows.Editor.ViewModels
{
    /// <summary>
    /// A run of the workflow drawn on the graph of the editor : where each node stands, the branches
    /// the run went through and how far it got. The run is the one started here, or one opened from
    /// the history, which is followed as it goes on when it is still running.
    /// <para>
    /// Held apart from the editor, which turns read only while a run is followed : what is drawn
    /// here belongs to that run rather than to the graph, and the next one clears it.
    /// </para>
    /// </summary>
    public partial class WorkflowRunViewModel : ObservableObject
    {
        /// <summary>The run drawn on the graph, null while none is.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanResume), nameof(CanStop), nameof(CanAnswer))]
        [NotifyCanExecuteChangedFor(nameof(StopCommand), nameof(ResumeCommand), nameof(AnswerCommand))]
        private TaskInstance? _displayedRun;

        /// <summary>Whether the displayed run is followed as it runs, the graph being read only meanwhile.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanResume), nameof(CanStop), nameof(CanAnswer))]
        [NotifyCanExecuteChangedFor(nameof(StartCommand), nameof(StopCommand), nameof(PauseCommand), nameof(ResumeCommand), nameof(AnswerCommand))]
        private bool _isRunning;

        /// <summary>A pause was asked for : the nodes running finish, no other starts.</summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(PauseCommand))]
        private bool _isPausing;

        /// <summary>
        /// Whether the displayed run waits to be resumed : paused, or cut short by a crash. Only the
        /// runs of an application are resumed from the editor, the supervisor resuming its own.
        /// </summary>
        public bool CanResume => !IsRunning
            && DisplayedRun is { State: EnumTaskState.Suspended, ApplicationId: not null };


        /// <summary>Whether there is a run to stop : the one followed, or one waiting to be resumed.</summary>
        public bool CanStop => IsRunning || CanResume;

        /// <summary>Whether a task of the displayed run asks the user in this application.</summary>
        public bool CanAnswer => DisplayedRun is TaskInstance run && _prompts.AskingFor(run.Id).Count > 0;

        /// <summary>
        /// How far the displayed run got, null while no run is displayed. Counted on the nodes it went
        /// through rather than on the graph : a graph is never walked whole, its branches leaving
        /// nodes out, so there is no total to progress towards.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasProgress))]
        private string? _progress;

        public bool HasProgress => !string.IsNullOrEmpty(Progress);

        private readonly AutomationWorkflow _workflow;
        private readonly IReadOnlyCollection<NodeViewModel> _nodes;
        private readonly IReadOnlyCollection<ConnectionViewModel> _connections;

        /// <summary>
        /// Persist the workflow and tell whether it can be started. What runs is read back from the
        /// storage, so a graph that couldn't be written would be followed node by node against a run
        /// of another version of itself.
        /// </summary>
        private readonly Func<Task<bool>> _saveAsync;

        private readonly IExecutionService _execution;
        private readonly UserPrompts _prompts;
        private readonly IHistoryService _history;
        private readonly IToastService _toasts;

        /// <summary>
        /// Instances reported while the run is being set up : the first nodes of a starting run are
        /// usually reported before the start hands over the instance they hang under, and a run
        /// opened from the history keeps running while what it already did is read.
        /// </summary>
        private readonly List<TaskInstance> _heldReports = [];

        /// <summary>Whether the reports are held, see <see cref="_heldReports"/>.</summary>
        private bool _isHolding;

        /// <summary>
        /// The node each instance of the displayed run stands for : an instance only knows the ones
        /// it carries on from by their ids, which is what the branches are drawn from.
        /// </summary>
        private readonly Dictionary<Guid, Guid> _nodeOfInstance = [];

        public WorkflowRunViewModel(
            AutomationWorkflow workflow,
            IReadOnlyCollection<NodeViewModel> nodes,
            IReadOnlyCollection<ConnectionViewModel> connections,
            Func<Task<bool>> saveAsync,
            IExecutionService execution,
            UserPrompts prompts,
            IHistoryService history,
            IToastService toasts)
        {
            _workflow = workflow;
            _nodes = nodes;
            _connections = connections;
            _saveAsync = saveAsync;
            _execution = execution;
            _prompts = prompts;
            _history = history;
            _toasts = toasts;
        }

        /// <summary>
        /// Start the workflow, the graph turning read only until the execution is over. Returns as
        /// soon as the execution started, its end being reported by the history service.
        /// <para>
        /// A workflow expecting an input asks for it first, the run being cancelled when the user
        /// gives up on the settings. The graph is then saved : what runs is the persisted workflow,
        /// so what is displayed has to be what was persisted.
        /// </para>
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanStart))]
        private async Task Start()
        {
            JToken? settings = null;
            if (ParametersForm.IsExpectingParameters(_workflow))
            {
                settings = await StartSettingsViewModel.ShowAsync(_workflow);
                if (settings == null)
                    return;
            }

            if (!await _saveAsync())
                return;

            // What is displayed of a run belongs to it : the previous one is cleared rather than
            // left over the graph of the new one.
            Reset();
            Follow();
            _isHolding = true;
            try
            {
                // Started by id : what runs is the workflow just saved, read back by the executor.
                DisplayedRun = await _execution.StartAsync(_workflow.Id, settings);
            }
            catch (Exception exception)
            {
                Unfollow();
                _toasts.Error(exception.Message, $"The workflow '{_workflow.Metadata.Name}' could not be started");
                return;
            }
            finally
            {
                ReleaseHeldReports();
            }

            // The execution may already be over by the time it is awaited, its end then having been
            // reported before there was anything to match it against.
            EndIfOver();
        }

        private bool CanStart() => !IsRunning;

        /// <summary>
        /// Pause the run : the nodes running finish, then it stops until it is resumed. The graph
        /// only becomes editable again once the run reports itself as paused.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanPause))]
        private async Task Pause()
        {
            if (DisplayedRun is not TaskInstance run)
                return;

            IsPausing = true;
            try
            {
                await _execution.PauseAsync(run.Id);
            }
            catch (Exception exception)
            {
                IsPausing = false;
                _toasts.Error(exception.Message, $"The workflow '{_workflow.Metadata.Name}' could not be paused");
            }
        }

        private bool CanPause() => IsRunning && !IsPausing;

        /// <summary>
        /// Stop the run, every node it runs being canceled. A run waiting to be resumed is canceled
        /// too, given up on. The graph only becomes editable again once a running execution actually
        /// reports itself as finished.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanStop))]
        private async Task Stop()
        {
            if (DisplayedRun is not TaskInstance run)
                return;

            try
            {
                await _execution.CancelAsync(run.Id);
            }
            catch (Exception exception)
            {
                _toasts.Error(exception.Message, $"The workflow '{_workflow.Metadata.Name}' could not be stopped");
                return;
            }

            // Not followed, the run that waited to be resumed is read back as it ended.
            if (!IsRunning)
                DisplayedRun = await _history.GetAsync(run.Id) ?? run;
        }

        /// <summary>
        /// Resume the displayed run where it was suspended, following it as it goes on :
        /// what it already did stays drawn.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanResume))]
        private async Task Resume()
        {
            if (DisplayedRun is not TaskInstance run)
                return;

            Follow();
            try
            {
                DisplayedRun = await _execution.ResumeAsync(run.Id);
            }
            catch (Exception exception)
            {
                Unfollow();
                _toasts.Error(exception.Message, $"The workflow '{_workflow.Metadata.Name}' could not be resumed");
            }
        }

        /// <summary>
        /// Open the page of a task of the displayed run asking the user : the run carries on as soon
        /// as it is answered, the graph following it. Several asking, the first one opens.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanAnswer))]
        private async Task Answer()
        {
            if (DisplayedRun is not TaskInstance run)
                return;

            IReadOnlyList<PageRequest> asking = _prompts.AskingFor(run.Id);
            if (asking.Count == 0)
                return;
            await OpenAsync(asking[0]);
        }

        private async Task OpenAsync(PageRequest request)
        {
            try
            {
                await _prompts.OpenAsync(request);
            }
            catch (Exception exception)
            {
                _toasts.Error(exception.Message, $"'{request.Instance.NodeName}' could not be answered");
            }
        }

        /// <summary>The user following the run is the one it asks : the page opens right away.</summary>
        private void OnAsked(PageRequest request) => Ui.Post(() =>
        {
            if (IsRunning && DisplayedRun is TaskInstance run && request.RunId == run.Id)
                _ = OpenAsync(request);
        });

        private void OnAskingChanged() => Ui.Post(() =>
        {
            OnPropertyChanged(nameof(CanAnswer));
            AnswerCommand.NotifyCanExecuteChanged();
        });

        /// <summary>
        /// Draw [run], a run of the workflow opened from the history : what it did as the history
        /// holds it, then what it does as it goes on when it is still running. A run followed until
        /// then runs on, it is only not displayed anymore.
        /// </summary>
        public async Task ShowAsync(TaskInstance run)
        {
            Unfollow();
            Reset();
            DisplayedRun = run;

            bool isRunning = (run.State & EnumTaskState.Finished) == 0;
            if (isRunning)
                Follow();

            IReadOnlyList<TaskInstance> instances;
            _isHolding = true;
            try
            {
                instances = await _history.GetChildrenAsync(run.Id);
            }
            catch (Exception exception)
            {
                Unfollow();
                _toasts.Error(exception.Message, $"The run of '{_workflow.Metadata.Name}' could not be read");
                return;
            }
            finally
            {
                _isHolding = false;
            }

            // Oldest first, then what was reported while they were read : each node ends on its last state.
            foreach (TaskInstance instance in instances)
                Apply(instance);
            ReleaseHeldReports();
            EndIfOver();
        }

        /// <summary>
        /// Stop displaying the run, whether it is followed or not : the graph it is drawn on is
        /// about to be replaced. A run followed runs on, it is only not displayed anymore.
        /// </summary>
        public void Close()
        {
            Unfollow();
            Reset();
        }

        /// <summary>
        /// Clear what a run left on the graph : the states of the nodes, the path it took and how
        /// far it got.
        /// </summary>
        public void Reset()
        {
            foreach (NodeViewModel node in _nodes)
                node.Follow();

            foreach (ConnectionViewModel connection in _connections)
                connection.IsTraversed = false;

            _heldReports.Clear();
            _nodeOfInstance.Clear();
            DisplayedRun = null;
            Progress = null;
        }

        /// <summary>
        /// An instance changed while a run is being followed, reported by the thread executing it.
        /// </summary>
        private void OnInstanceReported(TaskInstance instance)
        {
            Ui.Dispatch(() =>
            {
                if (_isHolding)
                {
                    _heldReports.Add(instance);
                    return;
                }

                Apply(instance);
            });
        }

        /// <summary>Apply what was held while the run was being set up, in the order it was reported.</summary>
        private void ReleaseHeldReports()
        {
            _isHolding = false;
            List<TaskInstance> held = [.. _heldReports];
            _heldReports.Clear();
            foreach (TaskInstance instance in held)
                Apply(instance);
        }

        /// <summary>
        /// Display what [instance] tells of the displayed run : the run itself, which may end it, or
        /// one of its nodes, whose progress is drawn on the graph.
        /// </summary>
        private void Apply(TaskInstance instance)
        {
            TaskInstance? run = DisplayedRun;
            if (run == null)
                return;

            if (run.Id == instance.Id)
            {
                DisplayedRun = instance;
                EndIfOver();
                return;
            }

            // Only the nodes of this very run, a workflow can be running in more than one place
            // (its own editor, a node of another graph, a schedule).
            if (run.Id != instance.ParentInstanceId || instance.NodeId is not Guid nodeId)
                return;

            NodeViewModel? node = _nodes.FirstOrDefault(x => x.Model.Id == nodeId);
            if (node == null)
                return;

            // The instance is only timed once it is over, what it holds meanwhile being the last
            // change of state rather than an end.
            bool finished = (instance.State & EnumTaskState.Finished) != 0;
            node.Follow(
                instance.State,
                finished && instance.FinishedAt is DateTime end ? end - instance.CreatedAt : null,
                instance.State == EnumTaskState.Failed ? FirstLine(instance.Output?.ToString()) : null);

            _nodeOfInstance[instance.Id] = nodeId;
            foreach (Guid previousId in instance.PreviousIds)
            {
                if (_nodeOfInstance.TryGetValue(previousId, out Guid previousNodeId))
                    Traverse(previousNodeId, nodeId);
            }
            RefreshProgress();
        }

        /// <summary>
        /// Draw the branch the run reached [nodeId] through. Only the nodes it links are known, so
        /// every connection between the two is drawn : as far as the instance says, that is how the
        /// run came in.
        /// </summary>
        private void Traverse(Guid previousNodeId, Guid nodeId)
        {
            foreach (ConnectionViewModel connection in _connections)
            {
                if (connection.Source.Node.Model.Id == previousNodeId && connection.Target.Node.Model.Id == nodeId)
                    connection.IsTraversed = true;
            }
        }

        partial void OnIsPausingChanged(bool value) => RefreshProgress();

        /// <summary>
        /// How far the run got, read from the nodes it went through : what it finished, and what it
        /// still has running.
        /// </summary>
        private void RefreshProgress()
        {
            int finished = 0;
            int running = 0;
            int awaiting = 0;
            foreach (NodeViewModel node in _nodes)
            {
                if (node.State is not EnumTaskState state)
                    continue;

                if (state == EnumTaskState.AwaitingUser)
                    awaiting++;
                else if ((state & EnumTaskState.Finished) != 0)
                    finished++;
                else
                    running++;
            }

            List<string> parts = [];
            if (finished > 0 || running > 0)
                parts.Add($"{finished} done");
            if (running > 0)
                parts.Add($"{running} running");
            if (awaiting > 0)
                parts.Add($"{awaiting} waiting for you");
            string? progress = parts.Count == 0 ? null : string.Join(", ", parts);
            Progress = IsPausing ? $"{progress ?? "Nothing done"}, pausing" : progress;
        }

        /// <summary>The followed run is over : the graph turns editable again, and the user is told.</summary>
        private void EndIfOver()
        {
            if (!IsRunning || DisplayedRun is not TaskInstance run || (run.State & EnumTaskState.Finished) == 0)
                return;

            Unfollow();
            Report(run);
        }

        /// <summary>
        /// Tell how the run ended, the editor being the place it was followed from.
        /// </summary>
        private void Report(TaskInstance instance)
        {
            string name = _workflow.Metadata.Name;
            switch (instance.State)
            {
                case EnumTaskState.Completed:
                    _toasts.Success($"The workflow '{name}' has been executed.", "Workflow completed");
                    break;
                case EnumTaskState.Canceled:
                    _toasts.Warning($"The workflow '{name}' has been canceled.", "Workflow canceled");
                    break;
                case EnumTaskState.Suspended:
                    // The output of a suspended run tells why : a pause, or the process running it gone.
                    _toasts.Warning($"{instance.Output?.ToString()} Resume it from here or from its history.", $"Workflow '{name}' suspended");
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
        /// Follow the displayed run as it goes on, the graph turning read only. Added and updated
        /// both : a node is reported as it starts, then again as it ends.
        /// </summary>
        private void Follow()
        {
            if (IsRunning)
                return;

            _history.InstanceAdded += OnInstanceReported;
            _history.InstanceUpdated += OnInstanceReported;
            _prompts.Asked += OnAsked;
            _prompts.AskingChanged += OnAskingChanged;
            IsRunning = true;
        }

        /// <summary>
        /// Stop following the run, the graph becoming editable again. The states left on the nodes
        /// are kept : they are what the run amounted to.
        /// </summary>
        private void Unfollow()
        {
            _history.InstanceAdded -= OnInstanceReported;
            _history.InstanceUpdated -= OnInstanceReported;
            _prompts.Asked -= OnAsked;
            _prompts.AskingChanged -= OnAskingChanged;
            IsRunning = false;
            IsPausing = false;
        }
    }
}
