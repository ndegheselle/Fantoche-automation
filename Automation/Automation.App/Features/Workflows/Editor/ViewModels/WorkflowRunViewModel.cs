using Automation.App.Common;
using Automation.Shared.Data.Execution;
using Automation.Shared.Data.Scoped;
using Automation.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Joufflu.Feedback;
using Newtonsoft.Json.Linq;

namespace Automation.App.Features.Workflows.Editor.ViewModels
{
    /// <summary>
    /// A run of the workflow, followed on the graph of the editor it was started from : where each
    /// node stands, the branches the run went through and how far it got.
    /// <para>
    /// Held apart from the editor, which turns read only while a run is on : what is drawn here
    /// belongs to that run rather than to the graph, and a new run clears it.
    /// </para>
    /// </summary>
    public partial class WorkflowRunViewModel : ObservableObject
    {
        /// <summary>
        /// Execution being followed, null while nothing is running.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsRunning))]
        [NotifyCanExecuteChangedFor(nameof(StartCommand), nameof(CancelCommand))]
        private TaskInstance? _runningInstance;

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
        private readonly IHistoryService _history;
        private readonly IToastService _toasts;

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

        public WorkflowRunViewModel(
            AutomationWorkflow workflow,
            IReadOnlyCollection<NodeViewModel> nodes,
            IReadOnlyCollection<ConnectionViewModel> connections,
            Func<Task<bool>> saveAsync,
            AppServices services)
        {
            _workflow = workflow;
            _nodes = nodes;
            _connections = connections;
            _saveAsync = saveAsync;
            _execution = services.Execution;
            _history = services.History;
            _toasts = services.Toasts;
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
            if (StartSettingsViewModel.IsExpectingSettings(_workflow))
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

            // Added and updated both : a node is reported as it starts, then again as it ends.
            _isStarting = true;
            _history.InstanceAdded += OnInstanceReported;
            _history.InstanceUpdated += OnInstanceReported;
            try
            {
                // Started by id : what runs is the workflow just saved, read back by the executor.
                RunningInstance = await _execution.StartAsync(_workflow.Id, settings);
            }
            catch (Exception exception)
            {
                Stop();
                _toasts.Error(exception.Message, $"The workflow '{_workflow.Metadata.Name}' could not be started");
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

        private bool CanStart() => !IsRunning;

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
                _toasts.Error(exception.Message, $"The workflow '{_workflow.Metadata.Name}' could not be canceled");
            }
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

            _reportedBeforeStart.Clear();
            Progress = null;
        }

        /// <summary>
        /// An instance changed while a run is being followed, reported by the thread executing it.
        /// </summary>
        private void OnInstanceReported(TaskInstance instance)
        {
            Ui.Dispatch(() =>
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

            foreach (ConnectionViewModel connection in _connections)
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
            foreach (NodeViewModel node in _nodes)
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
            string name = _workflow.Metadata.Name;
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
            _history.InstanceAdded -= OnInstanceReported;
            _history.InstanceUpdated -= OnInstanceReported;
            RunningInstance = null;
        }
    }
}
