using Automation.App.Base;
using Automation.Shared.Supervisor;
using Automation.Shared.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Automation.App.Components.Data
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
