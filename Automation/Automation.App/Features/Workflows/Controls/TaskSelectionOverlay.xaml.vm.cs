using Automation.App.Common;
using Automation.Shared.Base;
using Automation.Shared.Data.Scoped;
using Automation.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Joufflu.Navigation;

namespace Automation.App.Features.Workflows.Controls
{
    /// <summary>
    /// Search among every task and workflow to pick one, for instance to add it to a graph.
    /// </summary>
    public partial class TaskSelectionViewModel : OverlayViewModel<BaseAutomationTask>
    {
        [ObservableProperty] private Paginated<BaseAutomationTask> _result = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ValidateCommand))]
        private BaseAutomationTask? _selected;

        public SearchedPagingViewModel Paging { get; }

        /// <summary>
        /// Element kept out of the results, a workflow not being able to contain itself.
        /// </summary>
        private readonly Guid? _excludedId;

        private readonly IScopedService _scoped;

        public TaskSelectionViewModel(IScopedService scoped, IOverlayService overlays, Guid? excludedId = null)
            : base(overlays)
        {
            _scoped = scoped;
            _excludedId = excludedId;
            Paging = new SearchedPagingViewModel(RefreshAsync);
            Options.Title = "Select a task or a workflow";

            _ = RefreshAsync();
        }

        /// <summary>
        /// Show the selection overlay and wait for the user to pick a task or a workflow,
        /// <see langword="null"/> when dismissed.
        /// </summary>
        public static Task<BaseAutomationTask?> ShowAsync(Guid? excludedId = null)
            => ShowAsync(new TaskSelectionViewModel(
                SpineViewModel.Instance.Scoped,
                SpineViewModel.Instance.Overlays,
                excludedId));

        public async Task RefreshAsync()
        {
            Paginated<BaseAutomationTask> page = await _scoped.SearchAsync(Paging.Search, Paging.Options);
            page.Items.RemoveAll(x => x.Id == _excludedId);
            Result = page;
        }

        [RelayCommand(CanExecute = nameof(CanValidate))]
        public void Validate()
        {
            if (Selected != null)
                Close(Selected);
        }

        private bool CanValidate() => Selected != null;
    }
}
