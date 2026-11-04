using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fantoche.App.Common;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Services;
using Joufflu.Navigation;

namespace Fantoche.App.Features.Workflows.Details.Controls
{
    /// <summary>
    /// Detail of one execution : what it ran with, what it produced, and the instances it ran in
    /// turn when it is a workflow (one per node of its graph).
    /// <para>
    /// A sub instance opens its own detail, so a nested workflow is walked down to the task that
    /// actually failed.
    /// </para>
    /// <para>
    /// A run of a workflow is acted upon from its editor (see <see cref="OpenExecutionCommand"/>) :
    /// resumed, stopped, its pages answered.
    /// </para>
    /// </summary>
    public partial class InstanceDetailViewModel : ObservableObject, IOverlayContent
    {
        public TaskInstance Instance { get; }
        public OverlayOptions Options { get; }

        /// <summary>How long the execution took, empty while it hasn't finished.</summary>
        public string Duration => Instance.FinishedAt == null
            ? ""
            : Durations.Format(Instance.FinishedAt.Value - Instance.CreatedAt);

        /// <summary>
        /// Parameters the execution ran with, the references of the graph already resolved.
        /// </summary>
        public string ParametersJson => Json.Format(Instance.Parameters);

        public string OutputJson => Json.Format(Instance.Output);

        /// <summary>A failed execution holds the error it failed with in place of an output.</summary>
        public string OutputLabel => Instance.State == EnumTaskState.Failed ? "Error" : "Output";

        /// <summary>
        /// The instances run by this one, empty for a task. They are read again on
        /// <see cref="RefreshCommand"/> : a workflow displayed while it runs keeps producing them.
        /// </summary>
        public ObservableCollection<TaskInstance> Children { get; } = [];

        public bool HasChildren => Children.Count > 0;

        /// <summary>Whether the execution is a run of a workflow, which opens on its graph.</summary>
        public bool IsWorkflow => Instance.WorkflowVersion != null;

        private readonly Func<TaskInstance, Task> _openRun;
        private readonly IHistoryService _history;
        private readonly IOverlayer _overlayer;

        /// <param name="openRun">Draw a run of a workflow on the graph of its editor.</param>
        public InstanceDetailViewModel(
            TaskInstance instance,
            Func<TaskInstance, Task> openRun,
            IHistoryService history,
            IOverlayer overlayer)
        {
            Instance = instance;
            _openRun = openRun;
            _history = history;
            _overlayer = overlayer;
            Options = new OverlayOptions() { Title = $"Execution - {instance.NodeName}" };

            Children.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasChildren));
        }

        /// <param name="openRun">Draw a run of a workflow on the graph of its editor.</param>
        public static async Task ShowAsync(TaskInstance instance, Func<TaskInstance, Task> openRun)
        {
            SpineViewModel shell = SpineViewModel.Instance;
            IOverlayer overlayer = shell.Overlayer;

            var viewModel = new InstanceDetailViewModel(instance, openRun, shell.History, overlayer);
            // Read before it is shown : the detail opens on what the execution amounts to rather
            // than filling in under the reader.
            await viewModel.RefreshAsync();
            await overlayer.ShowAsync(viewModel);
        }

        /// <summary>
        /// Read the instances this one ran again, so a workflow followed while it runs shows the
        /// nodes it reached since.
        /// </summary>
        [RelayCommand]
        public async Task RefreshAsync()
        {
            IReadOnlyList<TaskInstance> children = await _history.GetChildrenAsync(Instance.Id);

            Children.Clear();
            foreach (TaskInstance child in children)
                Children.Add(child);

            OnPropertyChanged(nameof(Duration));
            OnPropertyChanged(nameof(ParametersJson));
            OnPropertyChanged(nameof(OutputJson));
        }

        /// <summary>
        /// Open the detail of one of the instances this one ran, which holds its own if it is a
        /// nested workflow.
        /// </summary>
        [RelayCommand]
        private Task OpenChild(TaskInstance? child)
            => child == null ? Task.CompletedTask : ShowAsync(child, _openRun);

        /// <summary>
        /// Open the run on the graph of its workflow, where it is followed and its pages answered :
        /// the detail closes.
        /// </summary>
        [RelayCommand]
        private Task OpenExecution()
        {
            _overlayer.Validate(this);
            return _openRun(Instance);
        }

        [RelayCommand]
        private void Validate() => _overlayer.Validate(this);
    }
}
