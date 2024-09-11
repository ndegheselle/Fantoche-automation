using Automation.App.Base;
using Automation.App.Shared.ApiClients;
using Automation.App.Shared.ViewModels.Tasks;
using Automation.Shared;
using Joufflu.Shared;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
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
        private readonly TasksClient _client;
        private IModalContainer _modal => this.GetCurrentModalContainer();

        public TaskPage(TaskNode task)
        {
            _client = _app.ServiceProvider.GetRequiredService<TasksClient>();
            Task = task;
            InitializeComponent();
            LoadFullTask(Task.Id);
            HandleFocus();
        }

        public async void LoadFullTask(Guid taskId)
        {
            TaskNode? fullTask = await _client.GetByIdAsync(taskId);

            if (fullTask == null)
                throw new ArgumentException("Task not found");
            Task = fullTask;
        }

        private void HandleFocus()
        {
            switch (Task.FocusOn)
            {
                case EnumScopedTabs.Settings:
                    TaskTabControl.SelectedIndex = 2;
                    break;
            }
            Task.FocusOn = EnumScopedTabs.Default;
        }
    }
}
