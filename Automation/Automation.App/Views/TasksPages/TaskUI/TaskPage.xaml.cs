using Automation.App.Base;
using Automation.App.ViewModels.Tasks;
using Automation.App.Views.TasksPages.ScopeUI;
using Automation.Shared.Contracts;
using Automation.Supervisor.Client;
using Joufflu.Shared;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using static System.Formats.Asn1.AsnWriter;

namespace Automation.App.Views.TasksPages.TaskUI
{
    /// <summary>
    /// Logique d'interaction pour TaskPage.xaml
    /// </summary>
    public partial class TaskPage : UserControl, IPage
    {
        public INavigationLayout? Layout { get; set; }
        public ScopedTaskItem Scoped { get; set; }
        public TaskNode? Task { get; set; }

        private readonly App _app = (App)App.Current;
        private readonly ITaskClient _client;
        private readonly IModalContainer _modal;

        public TaskPage(ScopedTaskItem task)
        {
            _modal = _app.ServiceProvider.GetRequiredService<IModalContainer>();
            _client = _app.ServiceProvider.GetRequiredService<ITaskClient>();
            Scoped = task;
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

        #region UI Events
        private async void ButtonParameters_Click(object sender, RoutedEventArgs e)
        {
            if (Task == null)
                return;
            if (await _modal.Show(new TaskEditModal(Task)))
                Task = await _client.UpdateTaskAsync(Task);
        }
        #endregion
    }
}
