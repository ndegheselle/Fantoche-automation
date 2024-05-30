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

namespace Automation.App.Layout
{
    /// <summary>
    /// Logique d'interaction pour MainLayout.xaml
    /// </summary>
    public partial class MainLayout : UserControl, IPageContainer
    {
        // dependency property for the side menu
        public static readonly DependencyProperty SideMenuProperty = DependencyProperty.Register("SideMenu", typeof(FrameworkElement), typeof(MainLayout), new PropertyMetadata(null));
        public FrameworkElement SideMenu
        {
            get => (FrameworkElement)GetValue(SideMenuProperty);
            set => SetValue(SideMenuProperty, value);
        }

        public MainLayout()
        {
            InitializeComponent();
            this.DataContext = this;
            Navigation.Instance.CurrentContainer = this;
        }

        public void Show(FrameworkElement page)
        {
            MainContainer.Content = page;
        }
    }
}
