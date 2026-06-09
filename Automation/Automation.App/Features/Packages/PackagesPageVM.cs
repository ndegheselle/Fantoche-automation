using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automation.App.Services.UI;
using Avalonia.Collections;
using Automation.Shared.Base;
using Automation.Shared.Data.Execution;
using Automation.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShadUI;

namespace Automation.App.Features.Packages;

internal partial class PackagesPageVM : ObservableObject, INavigable
{
    private readonly IPackagesService _packagesService;

    private bool _suppressReload;
    private CancellationTokenSource? _cts;
    private readonly DialogManager _dialogManager;
    private readonly ToastDisplay _toasts;

    public PackagesPageVM(IPackagesService packagesService, 
        DialogManager dialogManager,
        ToastDisplay toastManager)
    {
        _toasts = toastManager;
        _packagesService = packagesService;
        _dialogManager = dialogManager;

        GroupedItems = new DataGridCollectionView(Items);
        GroupedItems.GroupDescriptions.Add(new DataGridPathGroupDescription("Identifier.Id"));
    }

    public ObservableCollection<PackageInfos> Items { get; } = new();

    /// <summary>
    /// Grouped view over <see cref="Items"/> that groups packages by their
    /// identifier so that each identifier exposes its list of versions.
    /// </summary>
    public DataGridCollectionView GroupedItems { get; protected set; }

    [ObservableProperty] private string _searchText = string.Empty;

    [ObservableProperty] private long _totalItems;

    [ObservableProperty] private int _currentPage = 1;

    [ObservableProperty] private int _pageSize = 100;

    [ObservableProperty] private bool _isLoading;

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
    private async Task AddPackages(IReadOnlyList<string>? files)
    {
        if (files is null || files.Count == 0)
            return;

        foreach (var file in files)
        {
            try
            {
                var added = await _packagesService.AddAsync(file);
                if (added.Warnings.Count > 0)
                    _toasts.Warning("Package created",  string.Join('\n', added.Warnings.Select(x => x.Message)));
                else
                    _toasts.Success("Package created successfully", $"The package {added.Infos.Identifier} has been created.");
            }
            catch (PackageValidationException ex)
            {
                _toasts.Error("Could not add the package", ex.Message);
            }
        }

        await RefreshAsync();
    }

    [RelayCommand]
    private void Remove(PackageInfos package)
    {
        _dialogManager
            .CreateDialog(
                "Are you absolutely sure?",
                $"This action cannot be undone. This will permanently remove the package " + package.Identifier)
            .WithPrimaryButton("Remove", () => _ = RemoveConfirmedAsync(package), DialogButtonStyle.Destructive)
            .WithCancelButton("Cancel")
            .WithMaxWidth(512)
            .Dismissible()
            .Show();
    }

    private async Task RemoveConfirmedAsync(PackageInfos package)
    {
        await _packagesService.RemoveAsync(package.Identifier.Id, package.Identifier.Version);
        _toasts.Success("Removed successfully", $"The package {package.Identifier} have been removed.");
        await RefreshAsync();
    }
}

/// <summary>
/// Design class for [PackagesPageVM]
/// </summary>
internal class PackagesPageVMDesign : PackagesPageVM
{
    public PackagesPageVMDesign() : base(null!, null!, null!)
    {
        Items.Add(new PackageInfos
        {
            Identifier = new PackageIdentifier { Id = "MyCompany.Utils", Version = new Version("1.0.0") },
            Description = "Utility helpers"
        });
        Items.Add(new PackageInfos
        {
            Identifier = new PackageIdentifier { Id = "MyCompany.Utils", Version = new Version("1.1.0") },
            Description = "Utility helpers"
        });
        Items.Add(new PackageInfos
        {
            Identifier = new PackageIdentifier { Id = "MyCompany.Http", Version = new Version("2.0.0") },
            Description = "HTTP client wrappers"
        });
        Items.Add(new PackageInfos
        {
            Identifier = new PackageIdentifier { Id = "MyCompany.Logging", Version = new Version("3.1.4") },
            Description = "Structured logging"
        });
        
        // Rebuild the view AFTER items are added
        GroupedItems = new DataGridCollectionView(Items);
        GroupedItems.GroupDescriptions.Add(new DataGridPathGroupDescription("Identifier.Identifier"));

        TotalItems = 4;
        IsLoading = false;
    }
}