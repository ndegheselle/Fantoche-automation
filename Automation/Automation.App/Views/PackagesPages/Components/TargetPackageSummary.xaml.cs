using Automation.Shared.Data;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.PackagesPages.Components
{
    /// <summary>
    /// Logique d'interaction pour TargetPackageSummary.xaml
    /// </summary>
    public partial class TargetPackageSummary : UserControl
    {
        public static readonly DependencyProperty TargetedPackageProperty = DependencyProperty.Register(
            nameof(TargetPackage),
            typeof(TargetedPackageClass),
            typeof(TargetPackageSummary),
            new PropertyMetadata(null));

        public TargetedPackageClass? TargetPackage
        {
            get { return (TargetedPackageClass?)GetValue(TargetedPackageProperty); }
            set { SetValue(TargetedPackageProperty, value); }
        }

        public TargetPackageSummary()
        {
            InitializeComponent();
        }
    }
}
