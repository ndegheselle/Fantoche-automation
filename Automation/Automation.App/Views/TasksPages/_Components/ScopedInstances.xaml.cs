using Automation.Shared.ViewModels;
using Automation.Supervisor.Client;
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

            TotalInstances = await _scopeClient.GetScopedInstancesCountAsync(ScopedId);
        }

        public int TotalInstances { get; set; } = 0;
        public IEnumerable<TaskInstance> Instances { get; set; } = new List<TaskInstance>();

        private readonly App _app = (App)App.Current;
        private readonly IScopeClient _scopeClient;

        public ScopedInstances() {
            _scopeClient = _app.ServiceProvider.GetRequiredService<IScopeClient>();
            InitializeComponent();
        }

        private async void InstancesPaging_PagingChange(int pageNumber, int capacity)
        {
            if (ScopedId == Guid.Empty)
                return;
            Instances = await _scopeClient.GetScopedInstancesAsync(ScopedId, InstancesPaging.Capacity, InstancesPaging.PageNumber);
        }
    }
}
