using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Automation.App.Base;
using Automation.App.Services;
using Automation.Shared.Base;
using Automation.Shared.Data.Execution;
using Automation.Shared.Services;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShadUI;

namespace Automation.App.Features.Packages.Components;

/// <summary>
/// View model backing <see cref="TaskTargetPickerDialog"/>. Lets the user browse packages
/// (with search and pagination) and pick one of their task classes. On confirmation it
/// produces a <see cref="PackageClassTarget"/> exposed through <see cref="SelectedTarget"/>.
/// </summary>
internal partial class TaskTargetPickerVm : ViewModelBase
{
    private readonly IPackagesService _packagesService;

    private bool _suppressReload;
    private CancellationTokenSource? _searchCts;
    private CancellationTokenSource? _classesCts;

    public TaskTargetPickerVm(IPackagesService packagesService)
    {
        _packagesService = packagesService;

        GroupedClasses = new DataGridCollectionView(Classes);
        GroupedClasses.GroupDescriptions.Add(new DataGridPathGroupDescription(nameof(ClassTarget.Dll)));

        // Null in design-time (see TaskTargetPickerVMDesign), which pre-populates the lists itself.
        if (_packagesService != null)
            _ = RefreshAsync();
    }

    #region Packages (master)

    public ObservableCollection<PackageInfos> Packages { get; } = new();

    [ObservableProperty] private string _searchText = string.Empty;

    [ObservableProperty] private long _totalItems;

    [ObservableProperty] private int _currentPage = 1;

    [ObservableProperty] private int _pageSize = 50;

    [ObservableProperty] private bool _isLoadingPackages;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedPackage))]
    private PackageInfos? _selectedPackage;

    public bool HasSelectedPackage => SelectedPackage != null;

    partial void OnSearchTextChanged(string value) => ResetToFirstPageAndReload();

    partial void OnPageSizeChanged(int value) => ResetToFirstPageAndReload();

    partial void OnCurrentPageChanged(int value)
    {
        if (_suppressReload)
            return;
        _ = RefreshAsync();
    }

    partial void OnSelectedPackageChanged(PackageInfos? value)
    {
        SelectedClass = null;
        _ = LoadClassesAsync(value);
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
        _searchCts?.Cancel();
        var cts = new CancellationTokenSource();
        _searchCts = cts;
        var token = cts.Token;

        try
        {
            IsLoadingPackages = true;

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

            Packages.Clear();
            foreach (var p in result.Items)
                Packages.Add(p);
        }
        finally
        {
            // Only the most recent refresh clears the flag.
            if (ReferenceEquals(_searchCts, cts))
                IsLoadingPackages = false;
        }
    }

    #endregion

    #region Classes (detail)

    public ObservableCollection<ClassTarget> Classes { get; } = new();

    /// <summary>
    /// Grouped view over <see cref="Classes"/> that groups the package classes by
    /// the dll they belong to.
    /// </summary>
    public DataGridCollectionView GroupedClasses { get; }

    [ObservableProperty] private bool _isLoadingClasses;

    /// <summary>True once a package is selected, loading completed and it exposes no class.</summary>
    [ObservableProperty] private bool _isClassesEmpty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private ClassTarget? _selectedClass;

    private async Task LoadClassesAsync(PackageInfos? package)
    {
        _classesCts?.Cancel();
        var cts = new CancellationTokenSource();
        _classesCts = cts;
        var token = cts.Token;

        try
        {
            IsLoadingClasses = true;
            IsClassesEmpty = false;
            Classes.Clear();

            if (package == null || _packagesService == null)
                return;

            var classes = await _packagesService.GetClassesAsync(
                package.Identifier.Id, package.Identifier.Version);

            if (token.IsCancellationRequested)
                return;

            foreach (var target in classes)
                Classes.Add(target);

            IsClassesEmpty = Classes.Count == 0;
        }
        finally
        {
            if (ReferenceEquals(_classesCts, cts))
                IsLoadingClasses = false;
        }
    }

    #endregion

    #region Result

    /// <summary>
    /// The target built from the selected package and class, available after the dialog
    /// is confirmed.
    /// </summary>
    public PackageClassTarget? SelectedTarget { get; private set; }

    private bool CanConfirm() => SelectedPackage != null && SelectedClass != null;

    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private void Confirm()
    {
        if (SelectedPackage == null || SelectedClass == null)
            return;

        SelectedTarget = new PackageClassTarget(SelectedPackage.Identifier, SelectedClass.ClassFullName)
        {
            Dll = SelectedClass.Dll,
        };

        ServiceProvider.Dialogs.Close(this, new CloseDialogOptions { Success = true });
    }

    [RelayCommand]
    private void Cancel() => ServiceProvider.Dialogs.Close(this);

    #endregion
}

/// <summary>
/// Design class for <see cref="TaskTargetPickerVm"/>.
/// </summary>
internal class TaskTargetPickerVMDesign : TaskTargetPickerVm
{
    public TaskTargetPickerVMDesign() : base(null!)
    {
        Packages.Add(new PackageInfos
        {
            Identifier = new PackageIdentifier { Id = "MyCompany.Utils", Version = new Version("1.1.0") },
            Description = "Utility helpers"
        });
        Packages.Add(new PackageInfos
        {
            Identifier = new PackageIdentifier { Id = "MyCompany.Http", Version = new Version("2.0.0") },
            Description = "HTTP client wrappers"
        });

        var identifier = new PackageIdentifier { Id = "MyCompany.Utils", Version = new Version("1.1.0") };
        SelectedPackage = Packages[0];

        Classes.Add(new PackageClassTarget(identifier, "MyCompany.Utils.HttpTask") { Dll = "MyCompany.Utils.dll" });
        Classes.Add(new PackageClassTarget(identifier, "MyCompany.Utils.FileTask") { Dll = "MyCompany.Utils.dll" });
        Classes.Add(new PackageClassTarget(identifier, "MyCompany.Utils.Extra.MailTask") { Dll = "MyCompany.Utils.Extra.dll" });

        TotalItems = 2;
        IsLoadingPackages = false;
        IsLoadingClasses = false;
    }
}
