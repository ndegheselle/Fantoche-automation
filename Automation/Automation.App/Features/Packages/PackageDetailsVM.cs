using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Automation.App.Services.UI;
using Automation.Shared.Data.Execution;
using Automation.Shared.Services;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShadUI;

namespace Automation.App.Features.Packages;

internal partial class PackageDetailsVM : ObservableObject, INavigable
{
    private readonly IPackagesService _packagesService;
    private readonly NavigationManager _navigation;
    private readonly DialogManager _dialogManager;
    private readonly ToastDisplay _toasts;

    private Action? _onChanged;

    public PackageDetailsVM(IPackagesService packagesService,
        NavigationManager navigation,
        DialogManager dialogManager,
        ToastDisplay toasts)
    {
        _packagesService = packagesService;
        _navigation = navigation;
        _dialogManager = dialogManager;
        _toasts = toasts;

        GroupedClasses = new DataGridCollectionView(Classes);
        GroupedClasses.GroupDescriptions.Add(new DataGridPathGroupDescription("Dll"));
    }

    [ObservableProperty] private PackageInfos _package = new();

    [ObservableProperty] private bool _isLoading;

    /// <summary>
    /// True once loading completed and the package exposes no task class.
    /// </summary>
    [ObservableProperty] private bool _isEmpty;

    public ObservableCollection<TaskTarget> Classes { get; } = new();

    /// <summary>
    /// Grouped view over <see cref="Classes"/> that groups the package classes by
    /// the dll they belong to.
    /// </summary>
    public DataGridCollectionView GroupedClasses { get; }

    public ObservableCollection<Version> Versions { get; } = new();

    /// <param name="onChanged">
    /// Invoked when a version is removed so the caller can refresh its own listing.
    /// </param>
    public void Initialize(PackageInfos package, Action? onChanged = null)
    {
        Package = package;
        _onChanged = onChanged;
    }

    public void OnShow() => _ = LoadAsync();

    private async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            IsEmpty = false;
            Classes.Clear();
            Versions.Clear();

            var classes = await _packagesService.GetClassesAsync(
                Package.Identifier.Id, Package.Identifier.Version);

            foreach (var target in classes)
                Classes.Add(target);

            IsEmpty = Classes.Count == 0;

            var versions = await _packagesService.GetVersionsAsync(Package.Identifier.Id);
            foreach (var version in versions)
                Versions.Add(version);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Back() => _navigation.Close(this);

    [RelayCommand]
    private void RemoveVersion(Version version)
    {
        _dialogManager
            .CreateDialog(
                "Are you absolutely sure?",
                $"This action cannot be undone. This will permanently remove the version {version} of {Package.Identifier.Id}.")
            .WithPrimaryButton("Remove", () => _ = RemoveVersionConfirmedAsync(version), DialogButtonStyle.Destructive)
            .WithCancelButton("Cancel")
            .WithMaxWidth(512)
            .Dismissible()
            .Show();
    }

    private async Task RemoveVersionConfirmedAsync(Version version)
    {
        await _packagesService.RemoveAsync(Package.Identifier.Id, version);
        _toasts.Success("Removed successfully", $"The version {version} of {Package.Identifier.Id} has been removed.");

        _onChanged?.Invoke();

        Versions.Remove(version);

        // No version left means the package no longer exists, close the overlay.
        if (Versions.Count == 0)
            _navigation.Close(this);
    }
}

/// <summary>
/// Design class for [PackageDetailsVM]
/// </summary>
internal class PackageDetailsVMDesign : PackageDetailsVM
{
    public PackageDetailsVMDesign() : base(null!, null!, null!, null!)
    {
        Package = new PackageInfos
        {
            Identifier = new PackageIdentifier { Id = "MyCompany.Utils", Version = new System.Version("1.0.0") },
            Description = "Utility helpers"
        };

        Classes.Add(new PackageClassTarget(Package.Identifier, "MyCompany.Utils.HttpTask") { Dll = "MyCompany.Utils.dll" });
        Classes.Add(new PackageClassTarget(Package.Identifier, "MyCompany.Utils.FileTask") { Dll = "MyCompany.Utils.dll" });
        Classes.Add(new PackageClassTarget(Package.Identifier, "MyCompany.Utils.Extra.MailTask") { Dll = "MyCompany.Utils.Extra.dll" });

        Versions.Add(new System.Version("1.0.0"));
        Versions.Add(new System.Version("0.9.1"));
        Versions.Add(new System.Version("0.8.0"));

        IsLoading = false;
    }
}
