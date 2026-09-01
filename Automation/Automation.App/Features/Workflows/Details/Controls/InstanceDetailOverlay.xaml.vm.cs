using System.Collections.ObjectModel;
using Automation.Shared.Data.Execution;
using Automation.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Joufflu.Navigation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
    public partial class InstanceDetailViewModel : ObservableObject
    {
        public TaskInstance Instance { get; }

        public string Title => $"Execution - {Instance.NodeName}";

        /// <summary>
        /// How long the execution took, empty while it hasn't finished.
        /// </summary>
        public string Duration => Instance.FinishedAt == null
            ? ""
            : Format(Instance.FinishedAt.Value - Instance.CreatedAt);

        /// <summary>
        /// Parameters the execution ran with, the references of the graph already resolved.
        /// </summary>
        public string ParametersJson => Format(Instance.Parameters);

        public string OutputJson => Format(Instance.Output);

        /// <summary>
        /// The instances run by this one, empty for a task. They are read again on
        /// <see cref="RefreshCommand"/> : a workflow displayed while it runs keeps producing them.
        /// </summary>
        public ObservableCollection<TaskInstance> Children { get; } = [];

        public bool HasChildren => Children.Count > 0;

        private readonly IHistoryService _history;
        private readonly IOverlayService _overlays;

        public InstanceDetailViewModel(TaskInstance instance, IHistoryService history, IOverlayService overlays)
        {
            Instance = instance;
            _history = history;
            _overlays = overlays;

            Children.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasChildren));
        }

        /// <summary>
        /// Show the detail of [instance] and wait for it to be closed.
        /// </summary>
        public static async Task ShowAsync(TaskInstance instance)
        {
            IOverlayService overlays = SpineViewModel.Instance.Overlays;

            var viewModel = new InstanceDetailViewModel(instance, SpineViewModel.Instance.History, overlays);
            await viewModel.RefreshAsync();
            await overlays.Show(viewModel, new OverlayOptions() { Title = viewModel.Title });
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
        private void Close() => _overlays.CloseTop(true);

        /// <summary>
        /// A value of the execution as it is displayed : indented JSON, or the text itself when it
        /// isn't JSON (a failure is stored as its stack trace).
        /// </summary>
        private static string Format(JToken? token)
        {
            if (token == null)
                return "";

            return token.Type == JTokenType.String
                ? token.ToString()
                : token.ToString(Formatting.Indented);
        }

        private static string Format(TimeSpan duration)
        {
            if (duration < TimeSpan.FromSeconds(1))
                return $"{duration.TotalMilliseconds:0} ms";
            if (duration < TimeSpan.FromMinutes(1))
                return $"{duration.TotalSeconds:0.##} s";
            return duration.ToString(@"hh\:mm\:ss");
        }
    }
}
