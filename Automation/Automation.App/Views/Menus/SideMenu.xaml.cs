using Automation.App.ViewModels;
using Automation.App.ViewModels.Scopes;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.Menus
{
    /// <summary>
    /// Logique d'interaction pour SideMenu.xaml
    /// </summary>
    public partial class SideMenu : UserControl
    {
        private readonly SideMenuViewModel _sideMenuContext;
        private readonly App _app = (App)App.Current;

        public SideMenu()
        {
            _sideMenuContext = _app.ServiceProvider.GetRequiredService<SideMenuViewModel>();
            InitializeComponent();
            this.DataContext = _sideMenuContext;
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeView treeView = (TreeView)sender;
            ScopedElement? contextElement = treeView.SelectedItem as ScopedElement;

            if (contextElement == null)
                return;

            _sideMenuContext.SelectedElement = contextElement;
        }
    }
}
