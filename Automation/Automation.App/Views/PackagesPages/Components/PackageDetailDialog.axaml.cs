using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Automation.App.Views.PackagesPages.Components
{
    public partial class PackageDetailDialog : UserControl
    {
        public PackageDetailDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
    }
}
