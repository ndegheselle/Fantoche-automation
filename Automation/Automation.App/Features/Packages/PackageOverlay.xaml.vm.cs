using System.Collections.ObjectModel;
using Automation.Shared.Data.Execution;
using Automation.Shared.Services;
using CommunityToolkit.Mvvm.Input;
using Joufflu.Navigation;

namespace Automation.App.Features.Packages
{
    /// <summary>
    /// Detail of one package : the versions it is available in and the classes the current one
    /// holds, a version being removable from there.
    /// </summary>
    public partial class PackageViewModel : OverlayViewModel
    {
        public PackageInfos Package { get; }
        public ObservableCollection<Version> Versions { get; } = [];
        public ObservableCollection<ClassTarget> Classes { get; } = [];

        private readonly IPackagesService _packages;

        public PackageViewModel(PackageInfos package, IPackagesService packages, IOverlayService overlays)
            : base(overlays)
        {
            Package = package;
            _packages = packages;
            Options.Title = "Package detail";

            _ = RefreshAsync();
        }

        public async Task RefreshAsync()
        {
            Versions.Clear();
            foreach (var version in await _packages.GetVersionsAsync(Package.Identifier.Id))
                Versions.Add(version);

            Classes.Clear();
            foreach (var classe in await _packages.GetClassesAsync(Package.Identifier.Id, Package.Identifier.Version))
                Classes.Add(classe);
        }

        [RelayCommand]
        public async Task RemoveVersion(Version version)
        {
            if (await Overlays.Confirm($"Are you sure you want to remove the version '{version}' ?", "Confirm deletion") != true)
                return;

            await _packages.RemoveAsync(Package.Identifier.Id, version);
            Versions.Remove(version);
        }
    }
}
