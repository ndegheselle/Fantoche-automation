using System.Collections.ObjectModel;
using Automation.Shared.Base;
using Automation.Shared.Data.Execution;
using Automation.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Joufflu.Navigation;
using Joufflu.Navigation.Controls;
using Microsoft.Win32;

namespace Automation.App.Features.Packages
{
    public class PackagesViewModel : ObservableObject
    {
        private readonly IPackagesService _packages;
        private readonly ToastService _toasts;

        public ObservableCollection<PackageInfos> Packages { get; } = [];

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        private bool _isLoading;
        public bool IsLoading { get => _isLoading; private set => SetProperty(ref _isLoading, value); }

        public IAsyncRelayCommand SearchCommand { get; }
        public IAsyncRelayCommand AddCommand { get; }
        public IAsyncRelayCommand<PackageInfos> RemoveCommand { get; }

        public PackagesViewModel(IPackagesService packages, ToastService toasts)
        {
            _packages = packages;
            _toasts = toasts;

            SearchCommand = new AsyncRelayCommand(LoadAsync);
            AddCommand = new AsyncRelayCommand(AddAsync);
            RemoveCommand = new AsyncRelayCommand<PackageInfos>(RemoveAsync);

            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                var page = await _packages.SearchAsync(SearchText, new PaginationOptions { Page = 1, PageSize = 50 });
                Packages.Clear();
                foreach (var package in page.Items)
                    Packages.Add(package);
            }
            catch (Exception ex)
            {
                _toasts.Error($"Could not load packages: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task AddAsync()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Add a package",
                Filter = "NuGet package (*.nupkg)|*.nupkg|All files (*.*)|*.*",
                CheckFileExists = true,
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                var result = await _packages.AddAsync(dialog.FileName);
                if (result.Warnings.Count > 0)
                    _toasts.Warning($"Package '{result.Infos.Identifier.Id}' added with {result.Warnings.Count} warning(s).");
                else
                    _toasts.Success($"Package '{result.Infos.Identifier.Id}' added.");

                await LoadAsync();
            }
            catch (PackageValidationException ex)
            {
                _toasts.Error($"Invalid package: {ex.Message}");
            }
            catch (Exception ex)
            {
                _toasts.Error($"Could not add package: {ex.Message}");
            }
        }

        private async Task RemoveAsync(PackageInfos? package)
        {
            if (package == null)
                return;

            try
            {
                await _packages.RemoveAsync(package.Identifier.Id, package.Identifier.Version);
                Packages.Remove(package);
                _toasts.Success($"Package '{package.Identifier.Id}' removed.");
            }
            catch (Exception ex)
            {
                _toasts.Error($"Could not remove package: {ex.Message}");
            }
        }
    }
}
