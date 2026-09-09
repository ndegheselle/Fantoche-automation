using System.IO;
using System.Windows;
using Automation.App.Common;
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
        [ObservableProperty] private Paginated<PackageInfos> _result = new();

        public SearchedPagingViewModel Paging { get; }

        private readonly IPackagesService _packages;
        private readonly IOverlayService _overlays;
        private readonly IToastService _toasts;

        public PackagesViewModel(IPackagesService packages, IOverlayService overlays, IToastService toasts)
        {
            _packages = packages;
            _overlays = overlays;
            _toasts = toasts;
            Paging = new SearchedPagingViewModel(RefreshAsync);
        }

        [RelayCommand(CanExecute = nameof(CanDropFiles))]
        public void DropFiles(IDataObject? data)
        {
            foreach (string file in GetFiles(data) ?? [])
                _ = AddPackageAsync(file);
        }

        [RelayCommand]
        public void OpenPackage(PackageInfos package)
        {
            _overlays.Show(new PackageViewModel(package, _packages, _overlays));
        }

        public async Task AddPackageAsync(string filePath)
        {
            var result = await _packages.AddAsync(filePath);
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
