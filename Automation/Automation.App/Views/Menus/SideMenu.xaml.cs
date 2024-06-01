using Automation.App.Layout;
using Automation.App.ViewModels;
using Automation.Base;
using System;
using System.Collections.Generic;
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
            IContextElement? contextElement = treeView.SelectedItem as IContextElement;

            if (contextElement == null)
                return;

            SideMenuContext.SelectedElement = contextElement;
        }
    }
}
