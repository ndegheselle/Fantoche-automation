using System.IO;
using System.Windows;
using Fantoche.App.Common;
using Fantoche.Shared.Base;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Joufflu.Feedback;
using Joufflu.Navigation;

namespace Fantoche.App.Features.Packages
{
    public partial class PackagesViewModel : ObservableObject
    {
        [ObservableProperty] private Paginated<PackageInfos> _result = new();

        public SearchedPagingViewModel Paging { get; }

        private readonly IPackagesService _packages;
        private readonly IOverlayer _overlays;
        private readonly IToastService _toasts;

        public PackagesViewModel(IPackagesService packages, IOverlayer overlays, IToastService toasts)
        {
            _packages = packages;
            _overlays = overlays;
            _toasts = toasts;
            Paging = new SearchedPagingViewModel(RefreshAsync);
        }

        [RelayCommand(CanExecute = nameof(CanDropFiles))]
        public Task DropFiles(IDataObject? data)
        {
            return AddPackagesAsync(GetFiles(data) ?? []);
        }

        [RelayCommand]
        public async Task OpenPackage(PackageInfos package)
        {
            await _overlays.ShowAsync(new PackageViewModel(package, _packages, _overlays));
            // Versions may have been removed, the package itself with its last one.
            await RefreshAsync();
        }

        public async Task AddPackagesAsync(IEnumerable<string> filePaths)
        {
            foreach (string filePath in filePaths)
                await AddPackageAsync(filePath);
            await RefreshAsync();
        }

        private async Task AddPackageAsync(string filePath)
        {
            var toastInstance = _toasts.Loading("Adding the package ...", "Add package");
            var result = await _packages.AddAsync(filePath);
            if (result.Warnings.Any())
            {
                toastInstance.Type = ToastType.Warning;
                toastInstance.Message = string.Join("\n", result.Warnings.SelectMany(x => x.Message));
                toastInstance.Duration = TimeSpan.FromSeconds(5);
            }
            else
            {
                toastInstance.Type = ToastType.Success;
                toastInstance.Message = $"Package '{result.Infos.Identifier}' added.";
                toastInstance.Duration = TimeSpan.FromSeconds(5);
            }
        }

        public async Task RefreshAsync()
        {
            Result = await _packages.SearchAsync(Paging.Search, Paging.Options);
        }

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
