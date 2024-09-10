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
            DependencyProperty.Register(nameof(Package), typeof(Package), typeof(PackageInfos), new PropertyMetadata(null));

        public Package Package
        {
            get { return (Package)GetValue(MyPropertyProperty); }
            set { SetValue(MyPropertyProperty, value); }
        }

        public PackageInfos()
        {
            InitializeComponent();
        }
    }
}
