using System.Collections.ObjectModel;
using Automation.App.Common;
using Automation.Shared.Data.Execution;
using Automation.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Joufflu.Navigation;

namespace Automation.App.Features.Workflows.Details.Controls
{
    /// <summary>
    /// Detail of one execution : what it ran with, what it produced, and the instances it ran in
    /// turn when it is a workflow (one per node of its graph).
    /// <para>
    /// A sub instance opens its own detail, so a nested workflow is walked down to the task that
    /// actually failed.
    /// </para>
    /// </summary>
    public partial class InstanceDetailViewModel : OverlayViewModel
    {
        public TaskInstance Instance { get; }

        /// <summary>How long the execution took, empty while it hasn't finished.</summary>
        public string Duration => Instance.FinishedAt == null
            ? ""
            : Durations.Format(Instance.FinishedAt.Value - Instance.CreatedAt);

        /// <summary>
        /// Parameters the execution ran with, the references of the graph already resolved.
        /// </summary>
        public string ParametersJson => Json.Format(Instance.Parameters);

        public string OutputJson => Json.Format(Instance.Output);

        /// <summary>
        /// The instances run by this one, empty for a task. They are read again on
        /// <see cref="RefreshCommand"/> : a workflow displayed while it runs keeps producing them.
        /// </summary>
        public ObservableCollection<TaskInstance> Children { get; } = [];

        public bool HasChildren => Children.Count > 0;

        private readonly IHistoryService _history;

        public InstanceDetailViewModel(TaskInstance instance, IHistoryService history, IOverlayService overlays)
            : base(overlays)
        {
            Instance = instance;
            _history = history;
            Options.Title = $"Execution - {instance.NodeName}";

            Children.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasChildren));
        }

        public static async Task ShowAsync(TaskInstance instance)
        {
            IOverlayService overlays = SpineViewModel.Instance.Overlays;

            var viewModel = new InstanceDetailViewModel(instance, SpineViewModel.Instance.History, overlays);
            // Read before it is shown : the detail opens on what the execution amounts to rather
            // than filling in under the reader.
            await viewModel.RefreshAsync();
            await overlays.Show(viewModel);
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
            => child == null ? Task.CompletedTask : ShowAsync(child);

        [RelayCommand]
        private void Validate() => Close(true);
    }
}
