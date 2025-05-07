using Joufflu.Popups;
using Joufflu.Shared.Navigation;
using System.Windows.Controls;

namespace Automation.App.Views.PackagesPages
{
    /// <summary>
    /// Logique d'interaction pour PackagesMainPage.xaml
    /// </summary>
    public partial class PackagesMainPage : UserControl, IPage
    {
        private IModal _modal => this.GetCurrentModalContainer();
        public PackagesMainPage()
        {
            InitializeComponent();
        }
    }
}
