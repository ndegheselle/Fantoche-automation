using Automation.Shared.Base;
using Automation.Shared.Data.Execution;
using Automation.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Joufflu.Inputs.Controls;
using Joufflu.Navigation;

namespace Automation.App.Features.Packages
{
    public partial class PackagesViewModel : ObservableObject
    {
        [ObservableProperty]
        private Paginated<PackageInfos> result = new Paginated<PackageInfos>();

        private readonly IPackagesService packages;
        private readonly IOverlayService overlays;

        public PackagesViewModel(IPackagesService packages, IOverlayService overlays)
        {
            this.packages = packages;
            this.overlays = overlays;
        }

        public async void Search(string search, PaginationOptions options)
        {
            Result = await packages.SearchAsync(search, options);
        }

        public async void AddPackage(string filePaths)
        {
            await packages.AddAsync(filePaths);
        }

        [RelayCommand]
        public void OpenPackage()
        {
            overlays.Show(new PackageViewModel(), new OverlayOptions() { FullScreen = true, Title = "Package detail" });
        }
    }
}
