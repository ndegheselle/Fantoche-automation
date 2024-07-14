using Automation.Shared.Supervisor;
using Automation.Shared.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.TasksPages.Components
{
    /// <summary>
    /// Logique d'interaction pour TaskInstanceTable.xaml
    /// </summary>
    public partial class TaskInstanceTable : UserControl, INotifyPropertyChanged
    {
        // Dependency property for the task id
        public static readonly DependencyProperty TaskIdProperty = DependencyProperty.Register(
            nameof(TaskId),
            typeof(Guid),
            typeof(TaskInstanceTable),
            new PropertyMetadata(Guid.Empty, (o, e) => ((TaskInstanceTable)o).OnTaskIdChanged()));

        public Guid TaskId { get { return (Guid)GetValue(TaskIdProperty); } set { SetValue(TaskIdProperty, value); } }

        private async void OnTaskIdChanged()
        {
            if (TaskId == Guid.Empty)
                return;

            TotalInstances = await _nodeRepository.GetTaskInstancesCountAsync(TaskId);
        }

        public int TotalInstances { get; set; } = 0;
        public IEnumerable<TaskInstance> Instances { get; set; } = new List<TaskInstance>();

        private readonly App _app = (App)App.Current;
        private readonly INodeRepository _nodeRepository;

        public TaskInstanceTable() {
            _nodeRepository = _app.ServiceProvider.GetRequiredService<INodeRepository>();
            InitializeComponent();
        }

        private async void InstancesPaging_PagingChange(int pageNumber, int capacity)
        {
            Instances = await _nodeRepository.GetTaskInstancesAsync(TaskId, InstancesPaging.Capacity, InstancesPaging.PageNumber);
        }
    }
}
