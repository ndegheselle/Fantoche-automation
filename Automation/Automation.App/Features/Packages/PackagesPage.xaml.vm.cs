using Automation.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Joufflu.Navigation;
using Joufflu.Navigation.Controls;

namespace Automation.App.Features.Packages
{
    public partial class PackagesViewModel : ObservableObject
    {
        private readonly IPackagesService packages;
        private readonly IOverlayService overlays;

        public PackagesViewModel(IPackagesService packages, IOverlayService overlays)
        {
            this.packages = packages;
            this.overlays = overlays;
        }

        public void OpenPackage()
        {
            overlays.Show(new PackageViewModel(), new OverlayOptions() { FullScreen = true, Title = "Package detail" });
        }
    }
}
