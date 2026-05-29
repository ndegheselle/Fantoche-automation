using Automation.App.Components.Inputs;
using Automation.App.Services.Abstractions;
using Automation.App.Views.PackagesPages.Components;
using Automation.Shared.Data.Execution;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShadUI;

namespace Automation.App.Views.PackagesPages
{
    /// <summary>
    /// MIGRATION: the "Packages" page (was PackagesMainPage hosting PackageSelector). Searchable,
    /// paged package list via <see cref="IPackagesService"/>; add a package by uploading a .nupkg
    /// (FilePickerDialog -> CreateAsync); opening a package shows PackageDetailDialog.
    /// </summary>
    public partial class PackagesMainPageViewModel : ObservableObject
    {
        private readonly IPackagesService _packagesService;
        private readonly DialogManager _dialogManager;
        private readonly ToastManager _toastManager;

        [ObservableProperty]
        private IReadOnlyList<PackageInfos> _packages = [];

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private int _total;

        [ObservableProperty]
        private int _page = 1;

        [ObservableProperty]
        private int _pageSize = 50;

        public PackagesMainPageViewModel(IPackagesService packages, DialogManager dialogManager, ToastManager toastManager)
        {
            _packagesService = packages;
            _dialogManager = dialogManager;
            _toastManager = toastManager;
            _ = SearchAsync(1, PageSize);
        }

        partial void OnSearchTextChanged(string value) => _ = SearchAsync(1, PageSize);

        public async Task SearchAsync(int page, int pageSize)
        {
            try
            {
                var result = await _packagesService.SearchAsync(SearchText.Trim(), page, pageSize);
                Packages = result.Data;
                Total = (int)result.Total;
                Page = page;
                PageSize = pageSize;
            }
            catch (NotImplementedException)
            {
                Packages = [];
                Total = 0;
            }
        }

        [RelayCommand]
        private void Add()
        {
            var picker = new FilePickerDialogViewModel(_dialogManager, "New package", "Select a .nupkg file");
            _dialogManager.CreateDialog(picker)
                .Dismissible()
                .WithSuccessCallback(vm => { _ = CreatePackageAsync(picker.FilePath); })
                .Show();
        }

        private async Task CreatePackageAsync(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            try
            {
                await _packagesService.CreateAsync(filePath);
                _toastManager.CreateToast("Package created").DismissOnClick().ShowSuccess();
                await SearchAsync(1, PageSize);
            }
            catch (NotImplementedException)
            {
                _toastManager.CreateToast("Uploading is not available yet (pending data rework).")
                    .DismissOnClick().ShowWarning();
            }
        }

        /// <summary>Opens the detail dialog for a package (versions / classes / target selection).</summary>
        public void OpenDetail(PackageInfos package)
        {
            var vm = new PackageDetailDialogViewModel(_dialogManager, _toastManager, _packagesService, package);
            _dialogManager.CreateDialog(vm)
                .Dismissible()
                .WithSuccessCallback(detail =>
                {
                    // detail.SelectedClass is the chosen task target. Consumed by the (deferred)
                    // task-target selection flow; no-op for plain browsing.
                })
                .Show();
        }
    }
}
