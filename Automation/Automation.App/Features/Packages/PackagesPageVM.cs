using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Collections;
using Automation.Shared.Base;
using Automation.Shared.Data.Execution;
using Automation.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Automation.App.Features.Packages;

internal partial class PackagesPageVM : ObservableObject, INavigable
{
    private readonly IPackagesService _packagesService;

    private bool _suppressReload;
    private CancellationTokenSource? _cts;
    private readonly NavigationManager _navigation;

    public PackagesPageVM(IPackagesService packagesService, NavigationManager navigation)
    {
        _navigation =  navigation;
        _packagesService = packagesService;

        GroupedItems = new DataGridCollectionView(Items);
        GroupedItems.GroupDescriptions.Add(new DataGridPathGroupDescription("Identifier.Identifier"));
    }

    public ObservableCollection<PackageInfos> Items { get; } = new();

    /// <summary>
    /// Grouped view over <see cref="Items"/> that groups packages by their
    /// identifier so that each identifier exposes its list of versions.
    /// </summary>
    public DataGridCollectionView GroupedItems { get; }

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private long _totalItems;

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _pageSize = 100;

    [ObservableProperty]
    private bool _isLoading;

    public void OnShow() => _ = RefreshAsync();

    public void OnHide() => _cts?.Cancel();

    partial void OnSearchTextChanged(string value) => ResetToFirstPageAndReload();

    partial void OnPageSizeChanged(int value) => ResetToFirstPageAndReload();

    partial void OnCurrentPageChanged(int value)
    {
        if (_suppressReload)
            return;
        _ = RefreshAsync();
    }

    private void ResetToFirstPageAndReload()
    {
        // Setting CurrentPage = 1 would normally fire OnCurrentPageChanged and
        // reload; suppress that so we only fetch once below.
        _suppressReload = true;
        CurrentPage = 1;
        _suppressReload = false;

        _ = RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        // Cancel any in-flight refresh so a slow earlier response can't
        // overwrite a newer one.
        _cts?.Cancel();
        var cts = new CancellationTokenSource();
        _cts = cts;
        var token = cts.Token;

        try
        {
            IsLoading = true;

            var options = new PaginationOptions
            {
                Page = CurrentPage,
                PageSize = PageSize,
            };

            var result = await _packagesService.SearchAsync(SearchText?.Trim() ?? string.Empty, options);

            // SearchAsync has no CancellationToken, so we can't abort the call
            // itself — just discard the result if a newer request superseded us.
            if (token.IsCancellationRequested)
                return;

            TotalItems = result.Total;

            Items.Clear();
            foreach (var p in result.Items)
                Items.Add(p);
        }
        finally
        {
            // Only the most recent refresh clears the flag.
            if (ReferenceEquals(_cts, cts))
                IsLoading = false;
        }
    }

    [RelayCommand]
    private void Edit(PackageInfos package)
    {
        // edit selected...
    }

    [RelayCommand]
    private async Task Remove(PackageInfos package)
    {
        await _packagesService.RemoveAsync(package.Identifier.Identifier, package.Identifier.Version);
        await RefreshAsync();
    }
}