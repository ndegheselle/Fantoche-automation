using Automation.Shared.Data;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.PackagesPages.Components
{
    /// <summary>
    /// Logique d'interaction pour PackageSummary.xaml
    /// </summary>
    public partial class PackageSummary : UserControl
    {
        public static readonly DependencyProperty PackageProperty = DependencyProperty.Register(
            nameof(Package),
            typeof(PackageInfos),
            typeof(PackageSummary),
            new PropertyMetadata(null));

        public PackageInfos? Package
        {
            get { return (PackageInfos?)GetValue(PackageProperty); }
            set { SetValue(PackageProperty, value); }
        }

        public PackageSummary() { InitializeComponent(); }
    }
}
