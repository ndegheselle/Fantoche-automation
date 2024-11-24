using Automation.App.Shared.ApiClients;
using Automation.App.Shared.ViewModels.Tasks;
using Joufflu.Popups;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.TasksPages.TaskUI
{
    public class TaskExecuteModal : TaskExecute, IModalContentValidation
    {
        private readonly App _app = (App)App.Current;
        private readonly TasksClient _taskClient;

        public Modal? ParentLayout { get; set; }

        public ModalValidationOptions? Options => new ModalValidationOptions()
        {
            Title = "Crap",
            ValidButtonText = "Execute"
        };

        public TaskExecuteModal(TaskNode task)
        {
            _taskClient = _app.ServiceProvider.GetRequiredService<TasksClient>();
            Task = task;
            Options!.Title = $"Execute task - {task.Name}";
        }

        public async Task<bool> OnValidation()
        {
            await _taskClient.ExecuteAsync(Task.Id, JsonSerializer.Deserialize<object>(JsonSettings));
            return true;
        }
    }

    /// <summary>
    /// Logique d'interaction pour TaskExecute.xaml
    /// </summary>
    public partial class TaskExecute : UserControl
    {
        public string JsonSettings { get; set; }
        public static readonly DependencyProperty TaskProperty = DependencyProperty.Register(
            nameof(Task),
            typeof(TaskNode),
            typeof(TaskExecute),
            new PropertyMetadata(null));

        public TaskNode Task { get { return (TaskNode)GetValue(TaskProperty); } set { SetValue(TaskProperty, value); } }

        public TaskExecute() { InitializeComponent(); }
    }
}
