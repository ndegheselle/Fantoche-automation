using Automation.App.ViewModels;
using Automation.Base;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.Menus
{
    /// <summary>
    /// Logique d'interaction pour SideMenu.xaml
    /// </summary>
    public partial class SideMenu : UserControl
    {
        private readonly SideMenuContext SideMenuContext = GlobalContext.Instance.SideMenu;

        public SideMenu()
        {
            InitializeComponent();
            this.DataContext = SideMenuContext;
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeView treeView = (TreeView)sender;
            ScopedElement? contextElement = treeView.SelectedItem as ScopedElement;

            if (contextElement == null)
                return;

            SideMenuContext.SelectedElement = contextElement;
        }
    }
}
