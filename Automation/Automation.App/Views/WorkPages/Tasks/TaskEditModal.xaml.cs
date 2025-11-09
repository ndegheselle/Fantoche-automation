using Automation.App.Components.Inputs;
using Automation.App.Shared.ApiClients;
using Automation.App.Views.PackagesPages.Components;
using Automation.Models.Work;
using Automation.Shared.Base;
using Automation.Shared.Data;
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
        private readonly TasksClient _taskClient;

        public AutomationTask NewTask { get; set; }

        public TaskCreateModal(AutomationTask task) : base("Create new task")
        {
            _taskClient = Services.Provider.GetRequiredService<TasksClient>();
            NewTask = task;
            ValidateCommand = new DelegateCommand(Validate);
            BindValue(nameof(ScopedMetadata.Name), NewTask.Metadata);
        }

        public async void Validate()
        {
            NewTask.Errors.Clear();
            try
            {
                NewTask.Id = await _taskClient.CreateAsync(NewTask);
                ParentLayout?.Hide(true);
            } catch (ValidationException ex)
            {
                if (ex.Errors != null)
                    NewTask.Errors.Add(ex.Errors);
                return;
            }
        }
    }

    /// <summary>
    /// Logique d'interaction pour TaskEdit.xaml
    /// </summary>
    public partial class TaskEditModal : UserControl, INotifyPropertyChanged, IModalContent
    {
        private readonly TasksClient _taskClient;
        private readonly PackagesClient _pacakgeClient;

        private IAlert _alert => this.GetCurrentAlert();
        public IModal? ParentLayout { get; set; }

        public ModalOptions Options { get; private set; } = new ModalOptions();

        public AutomationTask Task { get; set; }

        public ICustomCommand ValidateCommand { get; private set; }

        public TaskEditModal(AutomationTask task)
        {
            Task = task;
            Options.Title = $"Edit - {task.Metadata.Name}";
            _taskClient = Services.Provider.GetRequiredService<TasksClient>();
            _pacakgeClient = Services.Provider.GetRequiredService<PackagesClient>();
            ValidateCommand = new DelegateCommand(Validate);
            InitializeComponent();
        }

        #region UI events
        private void RemovePackage_Click(object sender, RoutedEventArgs e) { Task.Target = null; }

        private async void SelectPackage_Click(object sender, RoutedEventArgs e)
        {
            PackageSelectorModal modal = new PackageSelectorModal();
            if (await ParentLayout!.Show(modal) && modal.SelectedTarget != null && modal.SelectedInfos != null)
            {
                Task.Target = modal.SelectedTarget;
            }
        }
        #endregion

        public async void Validate()
        {
            Task.Errors.Clear();
            try
            {
                await _taskClient.UpdateAsync(Task.Id, Task);
                _alert.Success("Task updated !");
                ParentLayout?.Hide(true);
            } catch (ValidationException ex)
            {
                if (ex.Errors != null)
                    Task.Errors.Add(ex.Errors);
                return;
            }
        }
    }
}
