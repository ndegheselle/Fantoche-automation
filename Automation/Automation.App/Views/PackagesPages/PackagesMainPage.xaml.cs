using Automation.App.Views.PackagesPages.Components;
using Automation.Shared.Packages;
using Joufflu.Popups;
using Joufflu.Shared.Layouts;
using System.IO.Packaging;
using System.Windows.Controls;

namespace Automation.App.Views.PackagesPages
{
    /// <summary>
    /// Logique d'interaction pour PackagesMainPage.xaml
    /// </summary>
    public partial class PackagesMainPage : UserControl, IPage
    {
        private IModal _modal => this.GetCurrentModalContainer();

        public ILayout? ParentLayout { get; set; }
        public PackagesMainPage()
        {
            InitializeComponent();
        }

        private void PackageSelector_PackageClicked(object sender, PackageInfos package)
        {
            _modal.Show(new PackageEditModal(package));
        }
    }
}
