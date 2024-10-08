using Automation.App.Views.PackagesPages.Components;
using Automation.Shared.Data;
using System.Windows;
using System.Windows.Controls;

namespace Automation.App.Views.PackagesPages._Components
{
    /// <summary>
    /// Logique d'interaction pour TargetPackageSummary.xaml
    /// </summary>
    public partial class TargetPackageSummary : UserControl
    {
        public static readonly DependencyProperty TargetedPackageProperty = DependencyProperty.Register(
            nameof(TargetPackage),
            typeof(TargetedPackage),
            typeof(TargetPackageSummary),
            new PropertyMetadata(null));

        public TargetedPackage? TargetPackage
        {
            get { return (TargetedPackage?)GetValue(TargetedPackageProperty); }
            set { SetValue(TargetedPackageProperty, value); }
        }

        public TargetPackageSummary()
        {
            InitializeComponent();
        }
    }
}
