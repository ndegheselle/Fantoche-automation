using Automation.Shared.Base;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.PackagesPages.Components
{
    /// <summary>
    /// Logique d'interaction pour PackageInfos.xaml
    /// </summary>
    public partial class PackageInfos : UserControl
    {
        public static readonly DependencyProperty MyPropertyProperty =
            DependencyProperty.Register(nameof(Package), typeof(Automation.Shared.Base.PackageInfos), typeof(PackageInfos), new PropertyMetadata(null));

        public Automation.Shared.Base.PackageInfos Package
        {
            get { return (Automation.Shared.Base.PackageInfos)GetValue(MyPropertyProperty); }
            set { SetValue(MyPropertyProperty, value); }
        }

        public PackageInfos()
        {
            InitializeComponent();
        }
    }
}
