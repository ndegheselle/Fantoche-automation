using Automation.App.Shared.ApiClients;
using Automation.Dal.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.WorkPages.Scopes.Components
{
    /// <summary>
    /// Logique d'interaction pour ScopedBreadcrumb.xaml
    /// </summary>
    public partial class ScopedBreadcrumb : UserControl
    {
        #region Dependency properties
        public static readonly DependencyProperty ScopeProperty =
            DependencyProperty.Register(
            nameof(Scope),
            typeof(Scope),
            typeof(ScopedBreadcrumb),
            new PropertyMetadata(null, (o, e) => ((ScopedBreadcrumb)o).OnScopedChanged()));
        #endregion

        /// <summary>
        /// Parent scope of the element we want the breadcrumb from
        /// </summary>
        public Scope Scope
        {
            get { return (Scope)GetValue(ScopeProperty); }
            set { SetValue(ScopeProperty, value); }
        }

        public event Action<Scope>? ScopeSelected;
        public ObservableCollection<Scope> Parents { get; } = [];

        
        private readonly ScopesClient _client;

        public ScopedBreadcrumb()
        {
            _client = Services.Provider.GetRequiredService<ScopesClient>();
            InitializeComponent(); 
        }

        private async void OnScopedChanged()
        {
            Parents.Clear();
            if (Scope == null || Scope.Id == Scope.ROOT_SCOPE_ID)
                return;

            var parents = await _client.GetParentScopes(Scope.Id);
            foreach (Scope parent in parents)
            {
                Parents.Add(parent);
            }

            // We include the scope in the list of parent
            Parents.Add(Scope);
        }

        private void ButtonScope_Click(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)sender;
            Scope? targetScope = element.DataContext as Scope;

            if (targetScope == null || targetScope == Scope)
                return;

            ScopeSelected?.Invoke(targetScope);
        }
    }
}
