using Joufflu.Shared;
using System.Windows.Controls;

namespace Automation.App.Views.PackagesPages
{
    /// <summary>
    /// Logique d'interaction pour PackagesMainPage.xaml
    /// </summary>
    public partial class PackagesMainPage : UserControl, IPage
    {
        public INavigationLayout? Layout { get; set; }
        public PackagesMainPage()
        {
            InitializeComponent();
        }
    }
}
