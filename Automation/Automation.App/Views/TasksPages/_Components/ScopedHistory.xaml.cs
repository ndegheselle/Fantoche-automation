using Automation.App.Shared.ApiClients;
using Automation.App.Shared.ViewModels.Tasks;
using Automation.Shared;
using Automation.Shared.Base;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Security.Policy;
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
        public static readonly DependencyProperty TargetIdProperty = DependencyProperty.Register(
            nameof(TargetId),
            typeof(Guid),
            typeof(ScopedHistory),
            new PropertyMetadata(default(Guid), (o, e) => ((ScopedHistory)o).OnScopedChange()));

        public ScopedElement TargetId { get { return (ScopedElement)GetValue(TargetIdProperty); } set { SetValue(TargetIdProperty, value); } }

        public EnumScopedType? Type { get; set; }

        private void OnScopedChange()
        {
            RefreshHistory(History.Page, History.PageSize);
        }

        private readonly App _app = (App)App.Current;
        private readonly HistoryClient _historyClient;

        public PageWrapper<TaskHistory> History { get; set; } = new PageWrapper<TaskHistory>() { PageSize = 50, Page = 1 };

        public ScopedHistory() {
            _historyClient = _app.ServiceProvider.GetRequiredService<HistoryClient>();
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
            if (TargetId == null || IsLoaded == false)
                return;

            if (Type == EnumScopedType.Task || Type == EnumScopedType.Workflow)
            {
                History = await _historyClient.GetByTaskAsync(TargetId, pageNumber, capacity);
            }
            else if (Type == EnumScopedType.Scope)
            {
                History = await _historyClient.GetByScopeAsync(TargetId, pageNumber, capacity);
            }
        }
    }
}
