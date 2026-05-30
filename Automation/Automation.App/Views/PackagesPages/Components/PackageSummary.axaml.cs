using Automation.Shared.Data.Execution;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Automation.App.Views.PackagesPages.Components
{
    /// <summary>Displays a package's identifier, version and description.</summary>
    public partial class PackageSummary : UserControl
    {
        public static readonly StyledProperty<PackageInfos?> PackageProperty =
            AvaloniaProperty.Register<PackageSummary, PackageInfos?>(nameof(Package));

        public PackageInfos? Package
        {
            get => GetValue(PackageProperty);
            set => SetValue(PackageProperty, value);
        }

        public PackageSummary()
        {
            InitializeComponent();
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
    }
}
