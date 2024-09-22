using Automation.Shared.Base;
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
            typeof(Automation.Shared.Base.PackageInfos),
            typeof(PackageSummary),
            new PropertyMetadata(null));

        public Automation.Shared.Base.PackageInfos Package
        {
            get { return (Automation.Shared.Base.PackageInfos)GetValue(MyPropertyProperty); }
            set { SetValue(MyPropertyProperty, value); }
        }

        public PackageSummary() { InitializeComponent(); }
    }
}
