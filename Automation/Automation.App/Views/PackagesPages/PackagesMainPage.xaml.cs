using Joufflu.Shared;
using Joufflu.Shared.Layouts;
using System.Windows.Controls;

namespace Automation.App.Views.PackagesPages
{
    /// <summary>
    /// Logique d'interaction pour PackagesMainPage.xaml
    /// </summary>
    public partial class PackagesMainPage : UserControl, IPage
    {
        public ILayout? ParentLayout { get; set; }
        public PackagesMainPage()
        {
            InitializeComponent();
        }
    }
}
