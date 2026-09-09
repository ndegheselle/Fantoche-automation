using System.Collections.ObjectModel;
using Automation.App.Common;
using Automation.Shared.Base;
using Automation.Shared.Data.Execution;
using Automation.Shared.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Joufflu.Navigation;

namespace Automation.App.Features.Packages.Controls
{
    public enum EnumPackageSelectionStep
    {
        Package,
        Class
    }

    /// <summary>
    /// Pick a class to run : a package first, then one of the classes a version of it holds.
    /// </summary>
    public partial class PackageSelectionViewModel : OverlayViewModel<PackageClassTarget>
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsPackageStep), nameof(IsClassStep))]
        private EnumPackageSelectionStep _step = EnumPackageSelectionStep.Package;

        public bool IsPackageStep => Step == EnumPackageSelectionStep.Package;
        public bool IsClassStep => Step == EnumPackageSelectionStep.Class;

        // Step 1 : package selection
        [ObservableProperty] private Paginated<PackageInfos> _result = new();

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

        private readonly IPackagesService _packages;

        public PackageSelectionViewModel(IPackagesService packages, IOverlayService overlays)
            : base(overlays)
        {
            _packages = packages;
            Paging = new SearchedPagingViewModel(RefreshAsync);
            Options.Title = "Select a class";

            _ = RefreshAsync();
        }

        /// <summary>
        /// Show the selection overlay and wait for a class to be picked, null when dismissed.
        /// </summary>
        public static Task<PackageClassTarget?> ShowAsync()
            => ShowAsync(new PackageSelectionViewModel(
                SpineViewModel.Instance.Packages,
                SpineViewModel.Instance.Overlays));

        public async Task RefreshAsync()
        {
            Result = await _packages.SearchAsync(Paging.Search, Paging.Options);
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
        public void Validate()
        {
            if (SelectedPackage == null || SelectedVersion == null || SelectedClass == null)
                return;

            Close(new PackageClassTarget()
            {
                ClassFullName = SelectedClass.ClassFullName,
                Dll = SelectedClass.Dll,
                Package = new PackageIdentifier()
                {
                    Id = SelectedPackage.Identifier.Id,
                    Version = SelectedVersion
                }
            });
        }

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
