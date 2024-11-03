using Automation.App.Shared.ApiClients;
using Automation.App.Shared.ViewModels.Tasks;
using Automation.Shared.Base;
using Automation.Shared.Data;
using Joufflu.Popups;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.TasksPages.Components.Instances
{
    /// <summary>
    /// Logique d'interaction pour TaskInstanceTable.xaml
    /// </summary>
    public partial class ScopedInstanceList : UserControl, INotifyPropertyChanged
    {
        // Dependency property for the task id
        public static readonly DependencyProperty TargetIdProperty = DependencyProperty.Register(
            nameof(TargetId),
            typeof(Guid?),
            typeof(ScopedInstanceList),
            new PropertyMetadata(default(Guid), (o, e) => ((ScopedInstanceList)o).OnScopedChange()));

        public Guid? TargetId { get { return (Guid?)GetValue(TargetIdProperty); } set { SetValue(TargetIdProperty, value); } }
        public EnumScopedType? Type { get; set; }

        private readonly App _app = (App)App.Current;
        private readonly TasksClient _tasksClient;
        private readonly ScopesClient _scopesClient;
        private IModal _modal => this.GetCurrentModalContainer();

        public ListPageWrapper<TaskInstance> Instances { get; set; } = new ListPageWrapper<TaskInstance>() { PageSize = 50, Page = 1 };

        public ScopedInstanceList() {
            _tasksClient = _app.ServiceProvider.GetRequiredService<TasksClient>();
            _scopesClient = _app.ServiceProvider.GetRequiredService<ScopesClient>();
            InitializeComponent();
        }

        private void OnScopedChange()
        {
            RefreshHistory(Instances.Page, Instances.PageSize);
        }

        private async void RefreshHistory(int pageNumber, int capacity)
        {
            if (TargetId == null)
                return;

            if (Type == EnumScopedType.Task || Type == EnumScopedType.Workflow)
            {
                Instances = await _tasksClient.GetInstancesAsync(TargetId.Value, pageNumber-1, capacity);
            }
            // TODO : scope instances
        }

        #region UI events
        private void InstancesPaging_PagingChange(int pageNumber, int capacity)
        {
            RefreshHistory(pageNumber, capacity);
        }

        private void DataGridDetail_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DataGrid dataGrid = (DataGrid)sender;
            var instance = dataGrid.SelectedItem as TaskInstance;

            if (instance == null)
                return;
            _modal.Show(new InstanceDetail(instance));
        }
        #endregion
    }
}
