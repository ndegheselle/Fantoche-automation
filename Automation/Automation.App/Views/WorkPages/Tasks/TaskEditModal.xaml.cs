using Automation.App.Components.Inputs;
using Automation.App.Shared.ApiClients;
using Automation.App.Shared.ViewModels.Work;
using Automation.App.Views.PackagesPages.Components;
using Automation.Shared.Base;
using Joufflu.Popups;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Usuel.Shared;

namespace Automation.App.Views.WorkPages.Tasks
{
    public class TaskCreateModal : TextBoxModal, IModalContent
    {
        private readonly App _app = (App)App.Current;
        private readonly TasksClient _taskClient;

        public AutomationTask NewTask { get; set; }

        public TaskCreateModal(AutomationTask task) : base("Create new task")
        {
            _taskClient = _app.ServiceProvider.GetRequiredService<TasksClient>();
            NewTask = task;
            ValidateCommand = new DelegateCommand(Validate);
            BindValue(nameof(Scope.Name), NewTask);
        }

        public async void Validate()
        {
            NewTask.ClearErrors();
            try
            {
                NewTask.Id = await _taskClient.CreateAsync(NewTask);
                ParentLayout?.Hide(true);
            } catch (ValidationException ex)
            {
                if (ex.Errors != null)
                    NewTask.AddErrors(ex.Errors);
                return;
            }
        }
    }

    /// <summary>
    /// Logique d'interaction pour TaskEdit.xaml
    /// </summary>
    public partial class TaskEditModal : UserControl, INotifyPropertyChanged, IModalContent
    {
        private readonly App _app = (App)App.Current;
        private readonly TasksClient _taskClient;
        private readonly PackagesClient _pacakgeClient;

        private IAlert _alert => this.GetCurrentAlertContainer();

        private IModal _modal => this.GetCurrentModalContainer();

        public Modal? ParentLayout { get; set; }

        public ModalOptions Options { get; private set; } = new ModalOptions();

        public AutomationTask Task { get; set; }

        public ICustomCommand ValidateCommand { get; private set; }

        public TaskEditModal(AutomationTask task)
        {
            Task = task;
            Options.Title = $"Edit task {task.Name}";
            _taskClient = _app.ServiceProvider.GetRequiredService<TasksClient>();
            _pacakgeClient = _app.ServiceProvider.GetRequiredService<PackagesClient>();
            ValidateCommand = new DelegateCommand(Validate);
            InitializeComponent();
        }

        #region UI events
        private void RemovePackage_Click(object sender, RoutedEventArgs e) { Task.Package = null; }

        private async void SelectPackage_Click(object sender, RoutedEventArgs e)
        {
            PackageSelectorModal modal = new PackageSelectorModal();
            if (await _modal.Show(modal) && modal.TargetPackage != null)
            {
                Task.Package = modal.TargetPackage;
            }
        }
        #endregion

        public async void Validate()
        {
            Task.ClearErrors();
            try
            {
                await _taskClient.UpdateAsync(Task.Id, Task);
                _alert.Success("Task updated !");
            } catch (ValidationException ex)
            {
                if (ex.Errors != null)
                    Task.AddErrors(ex.Errors);
                return;
            }
        }
    }
}
