using System.ComponentModel;
using System.Windows.Controls;

namespace Automation.App.Views.Menus
{
    public enum EnumNavigationPages
    {
        Work,
        Servers,
        Packages
    }

    /// <summary>
    /// Logique d'interaction pour NavigationSideMenu.xaml
    /// </summary>
    public partial class NavigationSideMenu : UserControl, INotifyPropertyChanged
    {
        public EnumNavigationPages CurrentPage { get; set; } = EnumNavigationPages.Work;
        public event Action<EnumNavigationPages>? NavigationChanged;
        public NavigationSideMenu()
        {
            InitializeComponent();
        }

        private void NavigationTasks_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            CurrentPage = EnumNavigationPages.Work;
            NavigationChanged?.Invoke(CurrentPage);
        }

        private void NavigationWorkers_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            CurrentPage = EnumNavigationPages.Servers;
            NavigationChanged?.Invoke(CurrentPage);
        }

        private void NavigationPackages_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            CurrentPage = EnumNavigationPages.Packages;
            NavigationChanged?.Invoke(CurrentPage);
        }
    }
}
