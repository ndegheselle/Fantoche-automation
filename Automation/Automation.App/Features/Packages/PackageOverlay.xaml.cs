using System.Windows.Controls;

namespace Automation.App.Features.Packages
{
    public partial class PackageOverlay : UserControl
    {
        public PackageViewModel ViewModel => (PackageViewModel)this.DataContext;

        public PackageOverlay()
        {
            InitializeComponent();
        }
    }
}
