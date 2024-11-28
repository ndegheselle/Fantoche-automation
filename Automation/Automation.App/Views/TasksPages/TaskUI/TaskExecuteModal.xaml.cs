using Automation.App.Shared.ApiClients;
using Automation.App.Shared.ViewModels.Tasks;
using Joufflu.Popups;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Windows.Controls;

namespace Automation.App.Views.TasksPages.TaskUI
{
    /// <summary>
    /// Logique d'interaction pour TaskExecute.xaml
    /// </summary>
    public partial class TaskExecuteModal : UserControl, IModalContentValidation
    {
        private readonly App _app = (App)App.Current;
        private readonly TasksClient _taskClient;

        public Modal? ParentLayout { get; set; }
        public ModalOptions Options => new ModalOptions() { Title = "Execute task" };

        public string JsonSettings { get; set; } = "";
        public TaskNode Task { get; set; }

        public TaskExecuteModal(TaskNode task) {
            _taskClient = _app.ServiceProvider.GetRequiredService<TasksClient>();
            Task = task;
            Options.Title = $"Execute task - {task.Name}";
            InitializeComponent(); 
        }

        public async Task OnValidation()
        {
            await _taskClient.ExecuteAsync(Task.Id, JsonSerializer.Deserialize<object>(JsonSettings));
        }
    }
}
