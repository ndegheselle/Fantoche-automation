using System.Windows.Controls;

namespace Automation.App.Features.Packages.Controls
{
    public partial class PackageSelectionOverlay : UserControl
    {
        public PackageSelectionViewModel ViewModel => (PackageSelectionViewModel)this.DataContext;

        public PackageSelectionOverlay()
        {
            InitializeComponent();
        }
    }
}
