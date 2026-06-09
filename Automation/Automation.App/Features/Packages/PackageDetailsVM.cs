using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Automation.Shared.Data.Execution;
using Automation.Shared.Services;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Automation.App.Features.Packages;

internal partial class PackageDetailsVM : ObservableObject
{
    private readonly IPackagesService _packagesService;

    public PackageDetailsVM(IPackagesService packagesService)
    {
        _packagesService = packagesService;

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

    public void Initialize(PackageInfos package)
    {
        Package = package;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            IsEmpty = false;
            Classes.Clear();

            var classes = await _packagesService.GetClassesAsync(
                Package.Identifier.Id, Package.Identifier.Version);

            foreach (var target in classes)
                Classes.Add(target);

            IsEmpty = Classes.Count == 0;
        }
        finally
        {
            IsLoading = false;
        }
    }
}

/// <summary>
/// Design class for [PackageDetailsVM]
/// </summary>
internal class PackageDetailsVMDesign : PackageDetailsVM
{
    public PackageDetailsVMDesign() : base(null!)
    {
        Package = new PackageInfos
        {
            Identifier = new PackageIdentifier { Id = "MyCompany.Utils", Version = new System.Version("1.0.0") },
            Description = "Utility helpers"
        };

        Classes.Add(new PackageClassTarget(Package.Identifier, "MyCompany.Utils.HttpTask") { Dll = "MyCompany.Utils.dll" });
        Classes.Add(new PackageClassTarget(Package.Identifier, "MyCompany.Utils.FileTask") { Dll = "MyCompany.Utils.dll" });
        Classes.Add(new PackageClassTarget(Package.Identifier, "MyCompany.Utils.Extra.MailTask") { Dll = "MyCompany.Utils.Extra.dll" });

        IsLoading = false;
    }
}
