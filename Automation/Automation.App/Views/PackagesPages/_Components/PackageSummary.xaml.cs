using Automation.Shared.Base;
using Automation.Shared.Packages;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.PackagesPages.Components
{
    /// <summary>
    /// Logique d'interaction pour PackageSummary.xaml
    /// </summary>
    public partial class PackageSummary : UserControl
    {
        public static readonly DependencyProperty MyPropertyProperty = DependencyProperty.Register(
            nameof(Package),
            typeof(PackageInfos),
            typeof(PackageSummary),
            new PropertyMetadata(null));

        public PackageInfos Package
        {
            get { return (PackageInfos)GetValue(MyPropertyProperty); }
            set { SetValue(MyPropertyProperty, value); }
        }

        public PackageSummary() { InitializeComponent(); }
    }
}
