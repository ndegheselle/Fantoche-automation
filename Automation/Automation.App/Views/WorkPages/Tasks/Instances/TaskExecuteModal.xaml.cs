using Automation.App.Shared.ApiClients;
using Automation.App.Shared.ViewModels.Work;
using Joufflu.Popups;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace Automation.App.Views.WorkPages.Tasks.Instances
{
    /// <summary>
    /// Logique d'interaction pour TaskExecute.xaml
    /// </summary>
    public partial class TaskExecuteModal : UserControl, IModalContent
    {
        private readonly App _app = (App)App.Current;
        private readonly TasksClient _taskClient;

        public Modal? ParentLayout { get; set; }
        public ModalOptions Options { get; private set; } = new ModalOptions() { Title = "Execute task" };

        public string JsonSettings { get; set; } = "";
        public AutomationTask Task { get; set; }

        public TaskExecuteModal(AutomationTask task) {
            _taskClient = _app.ServiceProvider.GetRequiredService<TasksClient>();
            Task = task;
            Options.Title = $"Execute task - {task.Name}";

            InitializeComponent(); 
        }

        private async void ButtonExecute_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            TaskInstance instance = await _taskClient.ExecuteAsync(Task.Id, JsonSettings);
            ParentLayout?.Hide(true);
            ParentLayout?.Show(new TaskProgressionModal(instance));
        }
    }
}
