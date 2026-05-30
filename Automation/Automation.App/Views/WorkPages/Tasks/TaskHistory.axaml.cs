using Automation.App.Services.Abstractions;
using Automation.Shared.Data.Scoped;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;

namespace Automation.App.Views.WorkPages.Tasks
{
    /// <summary>Execution-history control for a task (parallels ScopeHistory).</summary>
    public partial class TaskHistory : UserControl
    {
        public static readonly StyledProperty<BaseAutomationTask?> TaskProperty =
            AvaloniaProperty.Register<TaskHistory, BaseAutomationTask?>(nameof(Task));

        public BaseAutomationTask? Task
        {
            get => GetValue(TaskProperty);
            set => SetValue(TaskProperty, value);
        }

        private readonly TaskHistoryViewModel _viewModel;

        public TaskHistory()
        {
            _viewModel = new TaskHistoryViewModel(AppServices.Provider.GetRequiredService<ITasksService>());
            DataContext = _viewModel;
            InitializeComponent();
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == TaskProperty && Task is not null)
            {
                _viewModel.TaskId = Task.Id;
                _ = _viewModel.LoadAsync(_viewModel.Page, _viewModel.PageSize);
            }
        }

        private void Paging_PagingChange(int pageNumber, int capacity)
        {
            _ = _viewModel.LoadAsync(pageNumber, capacity);
        }
    }
}
