using Automation.App.Base;
using Automation.App.Components.Inputs;
using Automation.App.Shared.ApiClients;
using Automation.App.Shared.ViewModels.Tasks;
using Automation.Shared.Base;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using Automation.App;
using Automation.App.Views.PackagesPages.Components;

namespace Automation.App.Views.TasksPages.TaskUI
{
    public class TaskCreateModal : TextBoxModal, IModalContentValidate
    {
        private readonly App _app = (App)App.Current;
        private readonly TasksClient _taskClient;
        public TaskNode NewTask { get; set; }

        public TaskCreateModal(TaskNode task) : base("Create new task")
        {
            _taskClient = _app.ServiceProvider.GetRequiredService<TasksClient>();
            Options.ValidButtonText = "Create";
            NewTask = task;
            BindValue(nameof(Scope.Name), NewTask);
        }

        public async Task<bool> OnValidate()
        {
            NewTask.ClearErrors();
            try
            {
                NewTask.Id = await _taskClient.CreateAsync(NewTask);
            }
            catch (ValidationException ex)
            {
                if (ex.Errors != null)
                    NewTask.AddErrors(ex.Errors);
                return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Logique d'interaction pour TaskEdit.xaml
    /// </summary>
    public partial class TaskEdit : UserControl
    {
        private readonly App _app = (App)App.Current;
        private readonly TasksClient _taskClient;
        private IAlert _alert => this.GetCurrentAlertContainer();
        private IModalContainer _modal => this.GetCurrentModalContainer();

        public static readonly DependencyProperty TaskProperty =
            DependencyProperty.Register(nameof(Task), typeof(TaskNode), typeof(TaskEdit), new PropertyMetadata(null));

        public TaskNode Task
        {
            get { return (TaskNode)GetValue(TaskProperty); }
            set { SetValue(TaskProperty, value); }
        }

        public TaskEdit()
        {
            _taskClient = _app.ServiceProvider.GetRequiredService<TasksClient>();
            InitializeComponent();
        }

        #region UI events

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            Task.ClearErrors();
            try
            {
                await _taskClient.UpdateAsync(Task.Id, Task);
                _alert.Success("Task updated !");
            }
            catch (ValidationException ex)
            {
                if (ex.Errors != null)
                    Task.AddErrors(ex.Errors);
            }
        }

        private void RemovePackage_Click(object sender, RoutedEventArgs e)
        {
            Task.Package = null;
        }

        private async void SelectPackage_Click(object sender, RoutedEventArgs e)
        {
            PackageSelectorModal modal = new PackageSelectorModal();
            if (await _modal.Show(modal) && modal.SelectedPackage != null)
            {
                Task.Package = modal.SelectedPackage;
            }
        }

        #endregion
    }
}
