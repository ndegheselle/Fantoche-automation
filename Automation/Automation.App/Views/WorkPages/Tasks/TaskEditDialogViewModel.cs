using Automation.App.Services.Abstractions;
using Automation.Shared.Base;
using Automation.Shared.Data.Scoped;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShadUI;

namespace Automation.App.Views.WorkPages.Tasks
{
    /// <summary>
    /// MIGRATION: replaces TaskEditModal (metadata portion). Edits a task's metadata via MetadataEdit
    /// and saves through <see cref="ITasksService"/>. Schema / target / schedule editing is deferred
    /// (SchemaEdit is a separate, heavier port).
    /// </summary>
    public partial class TaskEditDialogViewModel : ObservableObject
    {
        private readonly DialogManager _dialogManager;
        private readonly ToastManager _toastManager;
        private readonly ITasksService _tasks;

        public BaseAutomationTask Task { get; }

        public ScopedMetadata Metadata => Task.Metadata;

        public TaskEditDialogViewModel(DialogManager dialogManager, ToastManager toastManager, ITasksService tasks, BaseAutomationTask task)
        {
            _dialogManager = dialogManager;
            _toastManager = toastManager;
            _tasks = tasks;
            Task = task;
        }

        [RelayCommand]
        private async Task Save()
        {
            try
            {
                await _tasks.UpdateAsync(Task.Id, Task);
                _toastManager.CreateToast("Task updated").DismissOnClick().ShowSuccess();
                _dialogManager.Close(this, new CloseDialogOptions { Success = true });
            }
            catch (ValidationException ex)
            {
                string message = ex.Errors == null
                    ? "Validation failed."
                    : string.Join("\n", ex.Errors.SelectMany(e => e.Value));
                _toastManager.CreateToast("Invalid task").WithContent(message).DismissOnClick().ShowError();
            }
            catch (NotImplementedException)
            {
                _toastManager.CreateToast("Saving is not available yet (pending data rework).")
                    .DismissOnClick().ShowWarning();
            }
        }

        [RelayCommand]
        private void Cancel() => _dialogManager.Close(this);
    }
}
