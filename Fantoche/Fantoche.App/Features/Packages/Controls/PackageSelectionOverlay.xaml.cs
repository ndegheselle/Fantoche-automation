using System.Windows.Controls;

namespace Fantoche.App.Features.Packages.Controls
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
