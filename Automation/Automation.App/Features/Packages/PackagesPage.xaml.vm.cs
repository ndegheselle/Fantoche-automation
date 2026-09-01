using System.IO;
using System.Windows;
using Automation.Shared.Base;
using Automation.Shared.Data.Execution;
using Automation.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Joufflu.Feedback;
using Joufflu.Navigation;

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
        private readonly IToastService _toasts;

        public PackagesViewModel(IPackagesService packages, IOverlayService overlays, IToastService toasts)
        {
            this._packages = packages;
            _overlays = overlays;
            _toasts = toasts;
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
            _overlays.Show(new PackageViewModel(package, _packages, _overlays), new OverlayOptions() { Title = "Package detail" });
        }

        public async void AddPackage(string filePaths)
        {
            var result = await _packages.AddAsync(filePaths);
            if (result.Warnings.Any())
            {
                _toasts.Warning(string.Join("\n", result.Warnings.SelectMany(x => x.Message)), "Package added with errors");
            }
            else
            {
                _toasts.Success($"Package '{result.Infos.Identifier}' added.", "Package added");
            }
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
        /// <summary>
        /// A package, or the symbols of one : both are dropped the same way, the symbols being
        /// stored next to the package they make debuggable.
        /// </summary>
        private static bool IsPackage(string path)
        {
            string extension = Path.GetExtension(path);
            return extension.Equals(".nupkg", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".snupkg", StringComparison.OrdinalIgnoreCase);
        }
    }
}
