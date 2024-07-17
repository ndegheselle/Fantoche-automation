using Automation.Shared.ViewModels;
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
        public TaskNode Task { get; set; }

        private readonly App _app = (App)App.Current;
        private readonly ITaskClient _client;

        public TaskPage(ScopedTask scope)
        {
            _client = _app.ServiceProvider.GetRequiredService<ITaskClient>();
            InitializeComponent();
            LoadTask(scope);
        }

        public async void LoadTask(ScopedTask scope)
        {
            // Load full workflow
            if (await _client.GetNodeAsync(scope.TaskId) is not TaskNode task)
                throw new ArgumentException("Task not found.");
            Task = task;
            Task.ParentScope = scope;
            this.DataContext = Task;
        }
    }
}
