using Automation.App.Views.PackagesPages.Components;
using Automation.Shared.Packages;
using Joufflu.Shared.Layouts;
using System.Windows.Controls;

namespace Automation.App.Views.PackagesPages
{
    /// <summary>
    /// Logique d'interaction pour PackagesMainPage.xaml
    /// </summary>
    public partial class PackagesMainPage : UserControl, IPage
    {
        private IDialogLayout _modal => this.GetCurrentModalContainer();

        public ILayout? ParentLayout { get; set; }
        public PackagesMainPage()
        {
            InitializeComponent();
        }

        private void PackageSelector_SelectedPackageChanged(object sender, PackageInfos? package)
        {
            if (package == null)
                return;

            // _modal.Show(new PackageEdit(package.Value));
        }
    }
}
