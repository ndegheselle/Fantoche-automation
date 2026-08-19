using Automation.Shared.Base;
using Automation.Shared.Data.Execution;
using Automation.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Joufflu.Navigation;
using System.IO;
using System.Windows;

namespace Automation.App.Features.Packages
{
    public partial class PackagesViewModel : ObservableObject
    {
        [ObservableProperty]
        private Paginated<PackageInfos> _result = new Paginated<PackageInfos>();

        [ObservableProperty] private string _search = "";
        [ObservableProperty] private int _pageNumber = 1;
        [ObservableProperty] private int _capacity = 50;

        private readonly IPackagesService _packages;
        private readonly IOverlayService _overlays;

        public PackagesViewModel(IPackagesService packages, IOverlayService overlays)
        {
            this._packages = packages;
            this._overlays = overlays;
        }

        [RelayCommand(CanExecute = nameof(CanDropFiles))]
        public void DropFiles(IDataObject? data)
        {
            foreach (string file in GetFiles(data) ?? [])
                AddPackage(file);
        }

        [RelayCommand]
        public void OpenPackage(PackageInfos package)
        {
            _overlays.Show(new PackageViewModel(package, _packages, _overlays), new OverlayOptions() { FullScreen = true, Title = "Package detail" });
        }

        public async void AddPackage(string filePaths)
        {
            await _packages.AddAsync(filePaths);
        }

        public async Task RefreshAsync()
        {
            Result = await _packages.SearchAsync(Search, new PaginationOptions() { Page = PageNumber, PageSize = Capacity });
        }

        partial void OnSearchChanged(string value)
        {
            _pageNumber = 1;
            _ = RefreshAsync();
        }

        partial void OnCapacityChanged(int value) => _ = RefreshAsync();

        partial void OnPageNumberChanged(int value) => _ = RefreshAsync();

        private static bool CanDropFiles(IDataObject? data)
        {
            string[]? files = GetFiles(data);
            return files?.Length > 0 && files.All(IsPackage);
        }

        private static string[]? GetFiles(IDataObject? data) => data?.GetData(DataFormats.FileDrop) as string[];
        private static bool IsPackage(string path) => Path.GetExtension(path).Equals(".nupkg", StringComparison.OrdinalIgnoreCase);
    }
}
