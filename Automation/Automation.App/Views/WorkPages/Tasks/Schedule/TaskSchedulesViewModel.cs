using System.Collections.ObjectModel;
using Automation.App.Services.Abstractions;
using Automation.Shared.Data;
using Automation.Shared.Data.Scoped;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShadUI;

namespace Automation.App.Views.WorkPages.Tasks.Schedules
{
    /// <summary>
    /// MIGRATION: task schedule editor (the original was an unfinished placeholder). Lists the task's
    /// cron schedules with add/edit/remove via ScheduleEditDialog; changes are written back to the
    /// task and persisted through <see cref="ITasksService.UpdateAsync"/> (best-effort while stubbed).
    /// </summary>
    public partial class TaskSchedulesViewModel : ObservableObject
    {
        private readonly DialogManager _dialogManager;
        private readonly ToastManager _toastManager;
        private readonly ITasksService _tasks;

        private BaseAutomationTask? _task;

        public ObservableCollection<Schedule> Schedules { get; } = [];

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(EditScheduleCommand), nameof(RemoveScheduleCommand))]
        private Schedule? _selected;

        public TaskSchedulesViewModel(DialogManager dialogManager, ToastManager toastManager, ITasksService tasks)
        {
            _dialogManager = dialogManager;
            _toastManager = toastManager;
            _tasks = tasks;
        }

        public void SetTask(BaseAutomationTask task)
        {
            _task = task;
            Schedules.Clear();
            foreach (Schedule schedule in task.Schedules)
                Schedules.Add(schedule);
        }

        [RelayCommand]
        private void AddSchedule() => OpenEdit(new Schedule(), isNew: true);

        private bool HasSelection() => Selected != null;

        [RelayCommand(CanExecute = nameof(HasSelection))]
        private void EditSchedule()
        {
            if (Selected is { } schedule)
                OpenEdit(schedule, isNew: false);
        }

        [RelayCommand(CanExecute = nameof(HasSelection))]
        private void RemoveSchedule()
        {
            if (Selected is not { } schedule)
                return;

            Schedules.Remove(schedule);
            Selected = null;
            _ = PersistAsync();
        }

        private void OpenEdit(Schedule schedule, bool isNew)
        {
            var vm = new ScheduleEditDialogViewModel(_dialogManager, schedule);
            _dialogManager.CreateDialog(vm)
                .Dismissible()
                .WithSuccessCallback(dialog =>
                {
                    if (isNew)
                        Schedules.Add(schedule);
                    _ = PersistAsync();
                })
                .Show();
        }

        private async Task PersistAsync()
        {
            if (_task is null)
                return;

            _task.Schedules = Schedules.ToList();
            try
            {
                await _tasks.UpdateAsync(_task.Id, _task);
            }
            catch (NotImplementedException)
            {
                _toastManager.CreateToast("Saving schedules is not available yet (pending data rework).")
                    .DismissOnClick().ShowWarning();
            }
        }
    }
}
