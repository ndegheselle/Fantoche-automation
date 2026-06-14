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
using Automation.Shared.Data.Scoped;
using Automation.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShadUI;

namespace Automation.App.Features.Packages;

internal partial class PackagesPageVm : ObservableObject, INavigable
{
    private readonly IPackagesService _packagesService;
    private readonly ITasksService _tasksService;
    private readonly IScopedService _scopedService;

    private bool _suppressReload;
    private CancellationTokenSource? _cts;
    private readonly DialogManager _dialogManager;
    private readonly ToastDisplay _toasts;
    private readonly NavigationManager _navigation;

    public PackagesPageVm(IPackagesService packagesService,
        ITasksService tasksService,
        IScopedService scopedService,
        DialogManager dialogManager,
        ToastDisplay toastManager,
        NavigationManager navigation)
    {
        _toasts = toastManager;
        _packagesService = packagesService;
        _tasksService = tasksService;
        _scopedService = scopedService;
        _dialogManager = dialogManager;
        _navigation = navigation;
    }

    public ObservableCollection<PackageInfos> Items { get; } = new();

    /// <summary>Scopes the user can drop the generated catalog tasks into.</summary>
    public ObservableCollection<Scope> Scopes { get; } = new();

    /// <summary>Scope the catalog tasks of newly added packages are created under.</summary>
    [ObservableProperty] private Scope? _selectedScope;
    
    [ObservableProperty] private string _searchText = string.Empty;

    [ObservableProperty] private long _totalItems;

    [ObservableProperty] private int _currentPage = 1;

    [ObservableProperty] private int _pageSize = 100;

    [ObservableProperty] private bool _isLoading;

    public void OnShow()
    {
        _ = RefreshAsync();
        _ = LoadScopesAsync();
    }

    private async Task LoadScopesAsync()
    {
        var previousId = SelectedScope?.Id;
        var scopes = await _scopedService.GetScopesAsync();
        Scopes.Clear();
        foreach (var scope in scopes)
            Scopes.Add(scope);

        // Clearing the list resets the bound selection, so restore it (or default).
        SelectedScope = Scopes.FirstOrDefault(s => s.Id == previousId) ?? Scopes.FirstOrDefault();
    }

    [RelayCommand]
    private async Task CreateScope(string name)
    {
        name = name?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(name))
            return;

        // Create the new scope inside the currently selected one (root when none).
        Guid parentId = SelectedScope?.Id ?? Scope.ROOT_SCOPE_ID;
        var created = (Scope)await _scopedService.CreateAsync(new Scope(name, parentId));
        Scopes.Add(created);
        SelectedScope = created;
    }

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

        // Catalog tasks of the added packages are created under the chosen scope.
        Guid scopeId = SelectedScope?.Id ?? Scope.ROOT_SCOPE_ID;

        foreach (var file in files)
        {
            try
            {
                var added = await _packagesService.AddAsync(file);

                // Building the read-only catalog tasks loads the package once; reuse the
                // result to tell whether it exposed any compatible task.
                var tasks = await _tasksService.SyncPackageAsync(added.Infos.Identifier.Id, scopeId);

                var warnings = added.Warnings.Select(x => x.Message).ToList();
                if (tasks.Count == 0)
                    warnings.Add("This package doesn't contain any compatible task.");

                if (warnings.Count > 0)
                    _toasts.Warning("Package created", string.Join('\n', warnings));
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
    private void ShowDetails(PackageInfos package)
    {
        var detailsVm = new PackageDetailsVm(_packagesService, _tasksService, _navigation, _dialogManager, _toasts, package);
        _navigation.Overlay(detailsVm, () => _ = RefreshAsync());
    }
}

/// <summary>
/// Design class for [PackagesPageVM]
/// </summary>
internal class PackagesPageVMDesign : PackagesPageVm
{
    public PackagesPageVMDesign() : base(null!, null!, null!, null!, null!, null!)
    {
        Scopes.Add(new Scope("Ingestion", Scope.ROOT_SCOPE_ID));
        Scopes.Add(new Scope("Reporting", Scope.ROOT_SCOPE_ID));
        SelectedScope = Scopes[0];

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

        TotalItems = 4;
        IsLoading = false;
    }
}