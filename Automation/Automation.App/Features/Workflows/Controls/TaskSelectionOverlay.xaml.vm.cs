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
    public partial class TaskSelectionViewModel : ObservableObject
    {
        [ObservableProperty] private Paginated<BaseAutomationTask> _result = new();

        [ObservableProperty] private string _search = "";
        [ObservableProperty] private int _pageNumber = 1;
        [ObservableProperty] private int _capacity = 50;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ValidateCommand))]
        private BaseAutomationTask? _selected;

        /// <summary>
        /// Picked task or workflow, only set once the selection is validated.
        /// </summary>
        public BaseAutomationTask? Selection { get; private set; }

        /// <summary>
        /// Element kept out of the results, a workflow not being able to contain itself.
        /// </summary>
        private readonly Guid? _excludedId;

        private readonly IScopedService _scoped;
        private readonly IOverlayService _overlays;

        public TaskSelectionViewModel(IScopedService scoped, IOverlayService overlays, Guid? excludedId = null)
        {
            _scoped = scoped;
            _overlays = overlays;
            _excludedId = excludedId;

            _ = RefreshAsync();
        }

        /// <summary>
        /// Show the selection overlay and wait for the user to pick a task or a workflow,
        /// <see langword="null"/> when dismissed.
        /// </summary>
        public static async Task<BaseAutomationTask?> ShowAsync(Guid? excludedId = null)
        {
            IOverlayService overlays = SpineViewModel.Instance.Overlays;

            var viewModel = new TaskSelectionViewModel(SpineViewModel.Instance.Scoped, overlays, excludedId);
            if (await overlays.Show(viewModel, new OverlayOptions() { Title = "Select a task or a workflow" }) != true)
                return null;
            return viewModel.Selection;
        }

        public async Task RefreshAsync()
        {
            Paginated<BaseAutomationTask> page = await _scoped.SearchAsync(Search, new PaginationOptions() { Page = PageNumber, PageSize = Capacity });
            page.Items.RemoveAll(x => x.Id == _excludedId);
            Result = page;
        }

        [RelayCommand(CanExecute = nameof(CanValidate))]
        public void Validate()
        {
            Selection = Selected;
            _overlays.CloseTop(true);
        }

        [RelayCommand]
        public void Cancel() => _overlays.CloseTop(false);

        private bool CanValidate() => Selected != null;

        partial void OnSearchChanged(string value)
        {
            _pageNumber = 1;
            _ = RefreshAsync();
        }

        partial void OnCapacityChanged(int value) => _ = RefreshAsync();

        partial void OnPageNumberChanged(int value) => _ = RefreshAsync();
    }
}
