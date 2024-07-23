using Automation.App.ViewModels.Tasks;
using Automation.Shared.Data;
using Automation.Supervisor.Client;
using Joufflu.Shared;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace Automation.App.Views.TasksPages.TaskUI
{
    /// <summary>
    /// Logique d'interaction pour TaskPage.xaml
    /// </summary>
    public partial class TaskPage : UserControl, IPage
    {
        public INavigationLayout? Layout { get; set; }
        public ScopedTaskItem Scoped { get; set; }
        public TaskNode Task { get; set; }

        private readonly App _app = (App)App.Current;
        private readonly ITaskClient _client;

        public TaskPage(ScopedTaskItem task)
        {
            _client = _app.ServiceProvider.GetRequiredService<ITaskClient>();
            InitializeComponent();
            LoadTask(task);
        }

        public async void LoadTask(ScopedTaskItem task)
        {
            TaskNode? fullTask = await _client.GetTaskAsync(task.TargetId);

            if (fullTask == null)
                throw new ArgumentException("Task not found");
            Task = fullTask;
        }
    }
}
