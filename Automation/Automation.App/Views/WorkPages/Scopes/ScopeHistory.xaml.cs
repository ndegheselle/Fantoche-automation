using Automation.App.Shared.ApiClients;
using Automation.App.Shared.ViewModels.Work;
using Automation.App.Views.WorkPages.Tasks;
using Automation.Shared.Base;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.WorkPages.Scopes
{
    /// <summary>
    /// Logique d'interaction pour ScopeHistory.xaml
    /// </summary>
    public partial class ScopeHistory : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty ScopeProperty = DependencyProperty.Register(
            nameof(Scope),
            typeof(Scope),
            typeof(ScopeHistory),
            new PropertyMetadata(null));

        public Scope Scope { get { return (Scope)GetValue(ScopeProperty); } set { SetValue(ScopeProperty, value); } }

        public ListPageWrapper<TaskInstance> Instances
        {
            get;
            set;
        } = new ListPageWrapper<TaskInstance>() { PageSize = 50, Page = 1 };

        private bool _isAlreadyRefreshed = false;
        
        private readonly ScopesClient _scopesClient;

        public ScopeHistory()
        {
            _scopesClient = Services.Provider.GetRequiredService<ScopesClient>();
            InitializeComponent();
            IsVisibleChanged += ScopeHistory_IsVisibleChanged;
        }

        private void ScopeHistory_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.IsVisible && _isAlreadyRefreshed == false)
            {
                RefreshHistory(Instances.Page, Instances.PageSize);
            }
        }

        private async void RefreshHistory(int pageNumber, int capacity)
        {
            if (Scope == null)
                return;
            Instances = await _scopesClient.GetInstancesAsync(Scope.Id, pageNumber - 1, capacity);
            _isAlreadyRefreshed = true;
        }

        private void InstancesPaging_PagingChange(int pageNumber, int capacity)
        { RefreshHistory(pageNumber, capacity); }
    }
}
