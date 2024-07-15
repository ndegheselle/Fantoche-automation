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
    public partial class ScopedInstances : UserControl, INotifyPropertyChanged
    {
        // Dependency property for the task id
        public static readonly DependencyProperty TaskIdProperty = DependencyProperty.Register(
            nameof(ScopedId),
            typeof(Guid),
            typeof(ScopedInstances),
            new PropertyMetadata(Guid.Empty, (o, e) => ((ScopedInstances)o).OnTaskIdChanged()));

        public Guid ScopedId { get { return (Guid)GetValue(TaskIdProperty); } set { SetValue(TaskIdProperty, value); } }

        private async void OnTaskIdChanged()
        {
            if (ScopedId == Guid.Empty)
                return;

            TotalInstances = await _scopeRepository.GetScopedInstancesCountAsync(ScopedId);
        }

        public int TotalInstances { get; set; } = 0;
        public IEnumerable<TaskInstance> Instances { get; set; } = new List<TaskInstance>();

        private readonly App _app = (App)App.Current;
        private readonly IScopeRepository _scopeRepository;

        public ScopedInstances() {
            _scopeRepository = _app.ServiceProvider.GetRequiredService<IScopeRepository>();
            InitializeComponent();
        }

        private async void InstancesPaging_PagingChange(int pageNumber, int capacity)
        {
            if (ScopedId == Guid.Empty)
                return;
            Instances = await _scopeRepository.GetScopedInstancesAsync(ScopedId, InstancesPaging.Capacity, InstancesPaging.PageNumber);
        }
    }
}
