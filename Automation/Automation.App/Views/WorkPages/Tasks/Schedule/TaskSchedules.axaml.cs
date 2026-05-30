using Automation.App.Services.Abstractions;
using Automation.Shared.Data.Scoped;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using ShadUI;

namespace Automation.App.Views.WorkPages.Tasks.Schedules
{
    /// <summary>Task schedule editor. Takes a task input; logic lives in <see cref="TaskSchedulesViewModel"/>.</summary>
    public partial class TaskSchedules : UserControl
    {
        public static readonly StyledProperty<BaseAutomationTask?> TaskProperty =
            AvaloniaProperty.Register<TaskSchedules, BaseAutomationTask?>(nameof(Task));

        public BaseAutomationTask? Task
        {
            get => GetValue(TaskProperty);
            set => SetValue(TaskProperty, value);
        }

        private readonly TaskSchedulesViewModel _viewModel;

        public TaskSchedules()
        {
            IServiceProvider provider = AppServices.Provider;
            _viewModel = new TaskSchedulesViewModel(
                provider.GetRequiredService<DialogManager>(),
                provider.GetRequiredService<ToastManager>(),
                provider.GetRequiredService<ITasksService>());
            DataContext = _viewModel;
            InitializeComponent();
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == TaskProperty && Task is not null)
                _viewModel.SetTask(Task);
        }

        private void ScheduleList_DoubleTapped(object? sender, TappedEventArgs e)
        {
            if (_viewModel.EditScheduleCommand.CanExecute(null))
                _viewModel.EditScheduleCommand.Execute(null);
        }
    }
}
