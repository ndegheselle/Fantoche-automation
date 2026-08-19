using Automation.Shared.Data.Execution;
using Automation.Shared.Services;
using CommunityToolkit.Mvvm.Input;
using Joufflu.Navigation;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace Automation.App.Features.Packages
{
    public partial class PackageViewModel
    {
        public PackageInfos Package { get; private set; }
        public ObservableCollection<Version> Versions { get; } = [];

        private readonly IPackagesService _packages;
        private readonly IOverlayService _overlays;

        public PackageViewModel(PackageInfos package, IPackagesService packages, IOverlayService overlays)
        {
            this.Package = package;
            this._packages = packages;
            this._overlays = overlays;
            Refresh();
        }

        private async void Refresh()
        {
            Versions.Clear();
            foreach (var version in await _packages.GetVersionsAsync(Package.Identifier.Id))
                Versions.Add(version);
        }

        [RelayCommand]
        public async Task RemoveVersion(Version version)
        {
            if (await _overlays.Confirm($"Are you sure you want to remove the version '{version}' ?", "Confirm deletion") != true)
                return;

            await _packages.RemoveAsync(Package.Identifier.Id, version);
            Versions.Remove(version);
        }
    }

    /// <summary>
    /// Logique d'interaction pour PackageOverlay.xaml
    /// </summary>
    public partial class PackageOverlay : UserControl
    {
        public PackageViewModel ViewModel => (PackageViewModel)this.DataContext;

        public PackageOverlay()
        {
            InitializeComponent();
        }
    }
}
