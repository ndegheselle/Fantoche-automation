using Automation.App.Services.Abstractions;
using Automation.App.ViewModels.Scoped;
using Automation.Shared.Data.Scoped;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShadUI;

namespace Automation.App.Views.WorkPages.Tasks
{
    /// <summary>
    /// MIGRATION: content shown when a task is opened (replaces TaskPage : IPage). Resolved to the
    /// TaskPage view by the ViewLocator. Hosts the execution-history tab and a settings action that
    /// opens TaskEditDialog. Schedules / Schema tabs are placeholders pending their ports.
    /// </summary>
    public partial class TaskPageViewModel : ObservableObject
    {
        private readonly DialogManager _dialogManager;
        private readonly ToastManager _toastManager;
        private readonly ITasksService _tasks;

        public TaskViewModel Task { get; }

        /// <summary>A task is fully configured once it has a target (workflows are always configured).</summary>
        public bool IsConfigured => Task.Model is not AutomationTask task || task.Target != null;

        public TaskPageViewModel(TaskViewModel task, DialogManager dialogManager, ToastManager toastManager, ITasksService tasks)
        {
            Task = task;
            _dialogManager = dialogManager;
            _toastManager = toastManager;
            _tasks = tasks;
        }

        [RelayCommand]
        public void OpenSettings()
        {
            var vm = new TaskEditDialogViewModel(_dialogManager, _toastManager, _tasks, Task.Model);
            _dialogManager.CreateDialog(vm).Dismissible().Show();
        }
    }
}
