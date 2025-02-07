using Automation.App.Shared.ApiClients;
using Automation.App.Shared.ViewModels.Work;
using Automation.App.Views.WorkPages.Tasks.Instances;
using Automation.Shared.Base;
using Joufflu.Popups;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.WorkPages.Tasks
{
    /// <summary>
    /// Logique d'interaction pour TaskHistory.xaml
    /// </summary>
    public partial class TaskHistory : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty TaskProperty =
            DependencyProperty.Register(
            nameof(Task),
            typeof(AutomationTask),
            typeof(TaskHistory),
            new PropertyMetadata(null));

        public AutomationTask Task { get { return (AutomationTask)GetValue(TaskProperty); } set { SetValue(TaskProperty, value); } }

        public ListPageWrapper<TaskInstance> Instances
        {
            get;
            set;
        } = new ListPageWrapper<TaskInstance>() { PageSize = 50, Page = 1 };

        private bool _isAlreadyLoaded = false;
        private readonly App _app = (App)App.Current;
        private readonly TasksClient _tasksClient;
        private IModal _modal => this.GetCurrentModalContainer();

        public TaskHistory()
        {
            _tasksClient = _app.ServiceProvider.GetRequiredService<TasksClient>();
            InitializeComponent();
            IsVisibleChanged += TaskHistory_IsVisibleChanged;
        }

        private void TaskHistory_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.IsVisible && _isAlreadyLoaded == false)
            {
                RefreshHistory(Instances.Page, Instances.PageSize);
            }
        }

        private async void RefreshHistory(int pageNumber, int capacity)
        {
            if (Task == null)
                return;
            Instances = await _tasksClient.GetInstancesAsync(Task.Id, pageNumber - 1, capacity);
            _isAlreadyLoaded = true;
        }

        private void InstancesPaging_PagingChange(int pageNumber, int capacity)
        { RefreshHistory(pageNumber, capacity); }

        #region UI events
        private void ButtonExecute_Click(object sender, RoutedEventArgs e)
        {
            _modal.Show(new TaskExecuteModal(Task));
        }
        #endregion
    }
}
