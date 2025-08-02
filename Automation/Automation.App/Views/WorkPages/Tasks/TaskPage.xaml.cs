using Automation.App.Shared.ApiClients;
using Automation.Dal.Models;
using Joufflu.Popups;
using Joufflu.Shared.Navigation;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Windows.Controls;

namespace Automation.App.Views.WorkPages.Tasks
{
    /// <summary>
    /// Logique d'interaction pour TaskPage.xaml
    /// </summary>
    public partial class TaskPage : UserControl, IPage, INotifyPropertyChanged
    {
        public AutomationTask Task { get; set; }
        public ILayout? ParentLayout { get; set; }

        
        private readonly TasksClient _client;
        private IModal _modal => this.GetCurrentModal();

        public TaskPage(AutomationTask task)
        {
            _client = Services.Provider.GetRequiredService<TasksClient>();
            Task = task;
            InitializeComponent();
            LoadFullTask(Task.Id);
            HandleFocus();
        }

        public async void LoadFullTask(Guid taskId)
        {
            AutomationTask? fullTask = await _client.GetByIdAsync(taskId) as AutomationTask;

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

        private void ButtonSettings_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _modal.Show(new TaskEditModal(Task));
            // TODO : maybe update the task in the side menu (different ref than the one modified in modal)
        }
    }
}
