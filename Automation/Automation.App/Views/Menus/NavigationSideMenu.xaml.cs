using System.ComponentModel;
using System.Windows.Controls;

namespace Automation.App.Views.Menus
{
    public enum EnumNavigationPages
    {
        Tasks,
        Workers
    }

    /// <summary>
    /// Logique d'interaction pour NavigationSideMenu.xaml
    /// </summary>
    public partial class NavigationSideMenu : UserControl, INotifyPropertyChanged
    {
        public EnumNavigationPages CurrentPage { get; set; } = EnumNavigationPages.Tasks;
        public event Action<EnumNavigationPages>? NavigationChanged;
        public NavigationSideMenu()
        {
            InitializeComponent();
        }

        private void NavigationTasks_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            CurrentPage = EnumNavigationPages.Tasks;
            NavigationChanged?.Invoke(CurrentPage);
        }

        private void NavigationWorkers_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            CurrentPage = EnumNavigationPages.Workers;
            NavigationChanged?.Invoke(CurrentPage);
        }
    }
}
