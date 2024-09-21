using Automation.App.Shared.ApiClients;
using Automation.App.Shared.ViewModels.Tasks;
using Joufflu.Shared.Layouts;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace Automation.App.Views.TasksPages.TaskUI
{
    /// <summary>
    /// Logique d'interaction pour TaskPage.xaml
    /// </summary>
    public partial class TaskPage : UserControl, IPage
    {
        public TaskNode Task { get; set; }
        public ILayout? ParentLayout { get; set; }

        private readonly App _app = App.Current;
        private readonly TasksClient _client;
        private IDialogLayout _modal => this.GetCurrentModalContainer();


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
