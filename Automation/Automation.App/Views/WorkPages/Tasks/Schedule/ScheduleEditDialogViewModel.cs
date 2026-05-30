using Automation.Shared.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShadUI;

namespace Automation.App.Views.WorkPages.Tasks.Schedules
{
    /// <summary>
    /// MIGRATION: replaces ScheduleEditModal (whose Validate threw). Edits a <see cref="Schedule"/>
    /// (cron + JSON settings). The view binds directly to the Schedule; Save just closes with success
    /// and the caller persists.
    /// </summary>
    public partial class ScheduleEditDialogViewModel : ObservableObject
    {
        private readonly DialogManager _dialogManager;

        public Schedule Schedule { get; }

        public ScheduleEditDialogViewModel(DialogManager dialogManager, Schedule schedule)
        {
            _dialogManager = dialogManager;
            Schedule = schedule;
        }

        [RelayCommand]
        private void Save() => _dialogManager.Close(this, new CloseDialogOptions { Success = true });

        [RelayCommand]
        private void Cancel() => _dialogManager.Close(this);
    }
}
