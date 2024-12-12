using Automation.App.Shared.ApiClients;
using Automation.App.Shared.ViewModels.Work;
using Automation.Shared.Base;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.WorkPages.Tasks.Instances
{
    /// <summary>
    /// Logique d'interaction pour TaskHistory.xaml
    /// </summary>
    public partial class TaskHistory : UserControl
    {
        public static readonly DependencyProperty TaskProperty =
            DependencyProperty.Register(
            nameof(Task),
            typeof(TaskNode),
            typeof(TaskHistory),
            new PropertyMetadata(null, (o, e) => ((TaskHistory)o).OnTaskChange()));

        public TaskNode Task { get { return (TaskNode)GetValue(TaskProperty); } set { SetValue(TaskProperty, value); } }

        public ListPageWrapper<TaskInstance> Instances
        {
            get;
            set;
        } = new ListPageWrapper<TaskInstance>() { PageSize = 50, Page = 1 };

        private readonly App _app = (App)App.Current;
        private readonly TasksClient _tasksClient;

        public TaskHistory()
        {
            _tasksClient = _app.ServiceProvider.GetRequiredService<TasksClient>();
            InitializeComponent();
        }

        private void OnTaskChange() { RefreshHistory(Instances.Page, Instances.PageSize); }

        private async void RefreshHistory(int pageNumber, int capacity)
        {
            if (Task == null)
                return;
            Instances = await _tasksClient.GetInstancesAsync(Task.Id, pageNumber - 1, capacity);
        }

        private void InstancesPaging_PagingChange(int pageNumber, int capacity)
        { RefreshHistory(pageNumber, capacity); }
    }
}
