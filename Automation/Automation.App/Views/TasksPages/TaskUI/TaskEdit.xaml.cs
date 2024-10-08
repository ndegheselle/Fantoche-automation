using Automation.App.Components.Inputs;
using Automation.App.Shared.ApiClients;
using Automation.App.Shared.ViewModels.Tasks;
using Automation.App.Views.PackagesPages.Components;
using Automation.Shared.Base;
using Automation.Shared.Data;
using Joufflu.Popups;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.TasksPages.TaskUI
{
    public class TaskCreateModal : TextBoxModal, IModalContentValidation
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

        public async Task<bool> OnValidation()
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
    public partial class TaskEdit : UserControl, INotifyPropertyChanged
    {
        private readonly App _app = (App)App.Current;
        private readonly TasksClient _taskClient;
        private readonly PackagesClient _pacakgeClient;
        private IAlert _alert => this.GetCurrentAlertContainer();
        private IModal _modal => this.GetCurrentModalContainer();

        public static readonly DependencyProperty TaskProperty =
            DependencyProperty.Register(nameof(Task), typeof(TaskNode), typeof(TaskEdit), new PropertyMetadata(null, (o, p) => ((TaskEdit)o).OnTaskChanged()));

        private async void OnTaskChanged()
        {
            if (Task?.Package == null || PackageInfos != null)
                return;

            PackageInfos = await _pacakgeClient.GetByIdAsync(Task.Package.Id);
        }

        public TaskNode Task
        {
            get { return (TaskNode)GetValue(TaskProperty); }
            set { SetValue(TaskProperty, value); }
        }

        public PackageInfos? PackageInfos { get; private set; }

        public TaskEdit()
        {
            _taskClient = _app.ServiceProvider.GetRequiredService<TasksClient>();
            _pacakgeClient = _app.ServiceProvider.GetRequiredService<PackagesClient>();

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
            PackageInfos = null;
        }

        private async void SelectPackage_Click(object sender, RoutedEventArgs e)
        {
            PackageSelectorModal modal = new PackageSelectorModal();
            if (await _modal.Show(modal) && modal.SelectedPackage != null)
            {
                Task.Package = new TargetedPackage() { Id = modal.SelectedPackage.Id };
                PackageInfos = modal.SelectedPackage;
            }
        }

        #endregion
    }
}
