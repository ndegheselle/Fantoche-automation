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

        /// <summary>
        /// The instances run by this one, empty for a task. They are read again on
        /// <see cref="RefreshCommand"/> : a workflow displayed while it runs keeps producing them.
        /// </summary>
        public ObservableCollection<TaskInstance> Children { get; } = [];

        public bool HasChildren => Children.Count > 0;

        private readonly IHistoryService _history;
        private readonly IOverlayer _overlayer;

        public InstanceDetailViewModel(TaskInstance instance, IHistoryService history, IOverlayer overlayer)
        {
            Instance = instance;
            _history = history;
            _overlayer = overlayer;
            Options = new OverlayOptions() { Title = $"Execution - {instance.NodeName}" };

            Children.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasChildren));
        }

        public static async Task ShowAsync(TaskInstance instance)
        {
            IOverlayer overlayer = SpineViewModel.Instance.Overlayer;

            var viewModel = new InstanceDetailViewModel(instance, SpineViewModel.Instance.History, overlayer);
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
            => child == null ? Task.CompletedTask : ShowAsync(child);

        [RelayCommand]
        private void Validate() => _overlayer.Validate(this);
    }
}
