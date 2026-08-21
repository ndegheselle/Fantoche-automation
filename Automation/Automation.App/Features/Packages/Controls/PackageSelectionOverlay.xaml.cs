using System.Collections.ObjectModel;
using System.Windows.Controls;
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

    public partial class PackageSelectionViewModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsPackageStep))]
        [NotifyPropertyChangedFor(nameof(IsClassStep))]
        private EnumPackageSelectionStep _step = EnumPackageSelectionStep.Package;

        public bool IsPackageStep => Step == EnumPackageSelectionStep.Package;
        public bool IsClassStep => Step == EnumPackageSelectionStep.Class;

        // Step 1 : package selection
        [ObservableProperty]
        private Paginated<PackageInfos> _result = new Paginated<PackageInfos>();

        [ObservableProperty] private string _search = "";
        [ObservableProperty] private int _pageNumber = 1;
        [ObservableProperty] private int _capacity = 50;

        // Step 2 : version and class selection
        [ObservableProperty]
        private PackageInfos? _selectedPackage;

        public ObservableCollection<Version> Versions { get; } = [];
        public ObservableCollection<ClassTarget> Classes { get; } = [];

        [ObservableProperty]
        private Version? _selectedVersion;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ValidateCommand))]
        private ClassTarget? _selectedClass;

        /// <summary>
        /// Selected class within the selected package version, only set once the selection is validated.
        /// </summary>
        public PackageClassTarget? Selection { get; private set; }

        private readonly IPackagesService _packages;
        private readonly IOverlayService _overlays;

        public PackageSelectionViewModel(IPackagesService packages, IOverlayService overlays)
        {
            this._packages = packages;
            this._overlays = overlays;

            _ = RefreshAsync();
        }

        /// <summary>
        /// Show the selection overlay and wait for the user to pick a class, <see langword="null"/> when dismissed.
        /// </summary>
        public static async Task<PackageClassTarget?> ShowAsync()
        {
            var overlays = SpineViewModel.Instance.Overlays;
            var packages = SpineViewModel.Instance.Packages;

            var viewModel = new PackageSelectionViewModel(packages, overlays);
            if (await overlays.Show(viewModel, new OverlayOptions() { Title = "Select a class" }) != true)
                return null;
            return viewModel.Selection;
        }

        public async Task RefreshAsync()
        {
            Result = await _packages.SearchAsync(Search, new PaginationOptions() { Page = PageNumber, PageSize = Capacity });
        }

        [RelayCommand]
        public async Task SelectPackage(PackageInfos package)
        {
            SelectedPackage = package;
            Step = EnumPackageSelectionStep.Class;

            Versions.Clear();
            foreach (var version in await _packages.GetVersionsAsync(package.Identifier.Id))
                Versions.Add(version);

            // Default to the package version, or the newest one when not available
            SelectedVersion = Versions.Contains(package.Identifier.Version)
                ? package.Identifier.Version
                : Versions.FirstOrDefault();
        }

        [RelayCommand]
        public void Back()
        {
            Step = EnumPackageSelectionStep.Package;
            SelectedPackage = null;
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

            Selection = new PackageClassTarget()
            {
                ClassFullName = SelectedClass.ClassFullName,
                Dll = SelectedClass.Dll,
                Package = new PackageIdentifier()
                {
                    Id = SelectedPackage.Identifier.Id,
                    Version = SelectedVersion
                }
            };
            _overlays.CloseTop(true);
        }

        [RelayCommand]
        public void Cancel() => _overlays.CloseTop(false);

        private bool CanValidate() => SelectedClass != null;

        private async void RefreshClasses()
        {
            Classes.Clear();
            SelectedClass = null;
            if (SelectedPackage == null || SelectedVersion == null)
                return;

            foreach (var classe in await _packages.GetClassesAsync(SelectedPackage.Identifier.Id, SelectedVersion))
                Classes.Add(classe);
        }

        partial void OnSelectedVersionChanged(Version? value) => RefreshClasses();

        partial void OnSearchChanged(string value)
        {
            _pageNumber = 1;
            _ = RefreshAsync();
        }

        partial void OnCapacityChanged(int value) => _ = RefreshAsync();

        partial void OnPageNumberChanged(int value) => _ = RefreshAsync();
    }

    /// <summary>
    /// Logique d'interaction pour PackageSelectionOverlay.xaml
    /// </summary>
    public partial class PackageSelectionOverlay : UserControl
    {
        public PackageSelectionViewModel ViewModel => (PackageSelectionViewModel)this.DataContext;

        public PackageSelectionOverlay()
        {
            InitializeComponent();
        }
    }
}
