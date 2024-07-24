using Automation.App.ViewModels.Tasks;
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
    public partial class ScopedHistory : UserControl, INotifyPropertyChanged
    {
        // Dependency property for the task id
        public static readonly DependencyProperty ScopedProperty = DependencyProperty.Register(
            nameof(Scoped),
            typeof(ScopedItem),
            typeof(ScopedHistory),
            new PropertyMetadata(default(ScopedItem), (o, e) => ((ScopedHistory)o).OnScopedChange()));

        public ScopedItem Scoped { get { return (ScopedItem)GetValue(ScopedProperty); } set { SetValue(ScopedProperty, value); } }

        private void OnScopedChange()
        {
            RefreshHistory(History.Page, History.PageSize);
        }

        private readonly App _app = (App)App.Current;
        private readonly IScopeClient _scopeClient;
        private readonly ITaskClient _taskClient;

        public TaskHistories History { get; set; } = new TaskHistories() { PageSize = 50, Page = 1 };

        public ScopedHistory() {
            _scopeClient = _app.ServiceProvider.GetRequiredService<IScopeClient>();
            _taskClient = _app.ServiceProvider.GetRequiredService<ITaskClient>();
            this.Loaded += ScopedHistory_Loaded;
            InitializeComponent();
        }

        private void ScopedHistory_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshHistory(History.Page, History.PageSize);
        }

        private void InstancesPaging_PagingChange(int pageNumber, int capacity)
        {
            RefreshHistory(pageNumber, capacity);
        }

        private async void RefreshHistory(int pageNumber, int capacity)
        {
            if (Scoped == null || IsLoaded == false)
                return;

            if (Scoped is ScopedTaskItem taskScoped)
            {
                History = await _taskClient.GetHistoryAsync(taskScoped.TargetId, pageNumber, capacity);
            }
            else if (Scoped is ScopeItem scopeScoped)
            {
                History = await _scopeClient.GetHistoryAsync(scopeScoped.TargetId, pageNumber, capacity);
            }
        }
    }
}
