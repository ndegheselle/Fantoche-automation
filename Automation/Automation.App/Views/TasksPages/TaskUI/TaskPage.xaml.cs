using Automation.App.Base;
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
        private readonly TaskClient _client;
        private readonly IModalContainer _modal;

        public TaskPage(TaskNode task)
        {
            _modal = _app.ServiceProvider.GetRequiredService<IModalContainer>();
            _client = _app.ServiceProvider.GetRequiredService<TaskClient>();
            Task = task;
            InitializeComponent();
            LoadTask(task);
        }

        public async void LoadTask(TaskNode task)
        {
            TaskNode? fullTask = await _client.GetByIdAsync(task.Id);

            if (fullTask == null)
                throw new ArgumentException("Task not found");
            Task = fullTask;
        }

        #region UI Events
        private async void ButtonParameters_Click(object sender, RoutedEventArgs e)
        {
            if (Task == null)
                return;
            if (await _modal.Show(new TaskEditModal(Task)))
                await _client.UpdateAsync(Task.Id, Task);
        }
        #endregion
    }
}
