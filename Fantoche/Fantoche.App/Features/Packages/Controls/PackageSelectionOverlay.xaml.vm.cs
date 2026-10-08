using System.Collections.ObjectModel;
using Fantoche.App.Common;
using Fantoche.Shared.Base;
using Fantoche.Shared.Data.Execution;
using Fantoche.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Joufflu.Navigation;

namespace Fantoche.App.Features.Packages.Controls
{
    public enum EnumPackageSelectionStep
    {
        Package,
        Class
    }

    /// <summary>
    /// Pick a class to run : a package first, then one of the classes a version of it holds.
    /// </summary>
    public partial class PackageSelectionViewModel : ObservableObject, IOverlayContent<PackageClassTarget>
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsPackageStep), nameof(IsClassStep))]
        private EnumPackageSelectionStep _step = EnumPackageSelectionStep.Package;

        public bool IsPackageStep => Step == EnumPackageSelectionStep.Package;
        public bool IsClassStep => Step == EnumPackageSelectionStep.Class;

        // Step 1 : package selection
        [ObservableProperty] private Paginated<PackageInfos> _found = new();

        public SearchedPagingViewModel Paging { get; }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(NextCommand))]
        private PackageInfos? _selectedPackage;

        // Step 2 : version and class selection
        public ObservableCollection<Version> Versions { get; } = [];
        public ObservableCollection<ClassTarget> Classes { get; } = [];

        [ObservableProperty] private Version? _selectedVersion;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ValidateCommand))]
        private ClassTarget? _selectedClass;

        public OverlayOptions Options { get; }

        /// <summary>The class picked, with the version it was picked from, null while incomplete.</summary>
        public PackageClassTarget? Result
            => SelectedPackage == null || SelectedVersion == null || SelectedClass == null
                ? null
                : new PackageClassTarget()
                {
                    ClassFullName = SelectedClass.ClassFullName,
                    Dll = SelectedClass.Dll,
                    Package = new PackageIdentifier()
                    {
                        Id = SelectedPackage.Identifier.Id,
                        Version = SelectedVersion
                    }
                };

        private readonly IPackagesService _packages;
        private readonly IOverlayer _overlayer;

        public PackageSelectionViewModel(IPackagesService packages, IOverlayer overlayer)
        {
            _packages = packages;
            _overlayer = overlayer;
            Paging = new SearchedPagingViewModel(RefreshAsync);
            Options = new OverlayOptions() { Title = "Select a class" };

            _ = RefreshAsync();
        }

        /// <summary>
        /// Show the selection overlay and wait for a class to be picked, null when dismissed.
        /// </summary>
        public static Task<PackageClassTarget?> ShowAsync()
            => SpineViewModel.Instance.Overlayer.ShowAsync(new PackageSelectionViewModel(
                SpineViewModel.Instance.Packages,
                SpineViewModel.Instance.Overlayer));

        public async Task RefreshAsync()
        {
            Found = await _packages.SearchAsync(Paging.Search, Paging.Options);
        }

        [RelayCommand(CanExecute = nameof(CanGoNext))]
        public async Task Next()
        {
            if (SelectedPackage == null)
                return;

            Step = EnumPackageSelectionStep.Class;

            Versions.Clear();
            foreach (var version in await _packages.GetVersionsAsync(SelectedPackage.Identifier.Id))
                Versions.Add(version);

            // Default to the package version, or the newest one when not available
            SelectedVersion = Versions.Contains(SelectedPackage.Identifier.Version)
                ? SelectedPackage.Identifier.Version
                : Versions.FirstOrDefault();
        }

        [RelayCommand]
        public void Back()
        {
            // Keep the selected package so the grid still shows it when going back
            Step = EnumPackageSelectionStep.Package;
            SelectedVersion = null;
            SelectedClass = null;
            Versions.Clear();
            Classes.Clear();
        }

        [RelayCommand(CanExecute = nameof(CanValidate))]
        public void Validate() => _overlayer.Validate(this);

        [RelayCommand]
        private void Cancel() => _overlayer.Cancel(this);

        private bool CanGoNext() => SelectedPackage != null;

        private bool CanValidate() => SelectedClass != null;

        private async Task RefreshClassesAsync()
        {
            Classes.Clear();
            SelectedClass = null;
            if (SelectedPackage == null || SelectedVersion == null)
                return;

            foreach (var classe in await _packages.GetClassesAsync(SelectedPackage.Identifier.Id, SelectedVersion))
                Classes.Add(classe);
        }

        partial void OnSelectedVersionChanged(Version? value) => _ = RefreshClassesAsync();
    }
}
