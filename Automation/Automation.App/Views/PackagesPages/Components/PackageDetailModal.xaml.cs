using AdonisUI.Controls;
using Automation.App.Components.Inputs;
using Automation.App.Shared.ApiClients;
using Automation.Shared.Base;
using Joufflu.Inputs;
using Joufflu.Popups;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using Usuel.Shared;

namespace Automation.App.Views.PackagesPages.Components
{
    /// <summary>
    /// Either create a new package or a package version if an existing package is passed
    /// </summary>
    public class PackageCreateModal : FilePickerModal, IModalContent, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string? propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public PackageInfos? Package { get; set; }

        private readonly PackagesClient _packagesClient;

        public PackageCreateModal(PackageInfos? package = null) : base(
            "New package",
            "Select a .nupkg file",
            new FilePickerOptions() { Filter = "Nuget package (*.nupkg)|*.nupkg", DefaultExtension = "*.nupkg" })
        {
            Package = package;
            _packagesClient = Services.Provider.GetRequiredService<PackagesClient>();
            ValidateCommand = new DelegateCommand(Validate, () => SelectedFile.HasErrors);
        }

        public bool IsInvalid()
        {
            SelectedFile.ClearErrors();
            // TODO : show error
            if (string.IsNullOrWhiteSpace(SelectedFile.FilePath) || !File.Exists(SelectedFile.FilePath))
            {
                SelectedFile.AddError("The selected file is invalid.", nameof(FilePickerFile.FilePath));
                return true;
            }
            return false;
        }

        public async void Validate()
        {
            if (IsInvalid())
                return;

            try
            {
                if (Package != null)
                {
                    Package = await _packagesClient.CreatePackageVersionAsync(Package.Identifier.Id, SelectedFile.FilePath);
                }
                else
                {
                    Package = await _packagesClient.CreateAsync(SelectedFile.FilePath);
                }
            } catch (ValidationException ex)
            {
                if (ex.Errors != null)
                    SelectedFile.AddErrors(ex.Errors);
                return;
            }
            ParentLayout?.Hide(true);
        }
    }

    /// <summary>
    /// Logique d'interaction pour PackageEdit.xaml
    /// </summary>
    public partial class PackageDetailModal : UserControl, INotifyPropertyChanged, IModalContent
    {
        public ModalOptions Options { get; } = new ModalOptions()
        {
            Title = "Select class"
        };
        public IModal? ParentLayout { get; set; }

        public PackageInfos Package { get; set; }
        public IEnumerable<Version> Versions { get; set; } = [];
        public IEnumerable<ClassIdentifier> PackageClasses { get; set; } = [];
        public Version SelectedVersion { get; set; }
        public ClassIdentifier? SelectedClass { get; set; } = null;

        private readonly PackagesClient _packagesClient;

        public PackageDetailModal(PackageInfos package)
        {
            _packagesClient = Services.Provider.GetRequiredService<PackagesClient>();
            Package = package;
            SelectedVersion = Package.Identifier.Version;
            InitializeComponent();
            LoadVersions();
        }

        private async void LoadVersions()
        {
            Versions = await _packagesClient.GetVersionsAync(Package.Identifier.Id);
            SelectedVersion = Versions.First();
            Package.Identifier.Version = SelectedVersion;
        }

        private async void LoadClasses()
        {
            PackageClasses = await _packagesClient.GetClassesAsync(Package.Identifier.Id, SelectedVersion);
        }

        #region UI events

        private async void ButtonAdd_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var createPackage = new PackageCreateModal(Package);
            if (await ParentLayout!.Show(createPackage) && createPackage.Package != null)
            {
                Package = createPackage.Package;
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadClasses();
        }

        private async void ButtonRemove_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (MessageBox.Show(
                    $"Are you sure you want to remove the version '{SelectedVersion}' ?",
                    "Confirmation",
                    AdonisUI.Controls.MessageBoxButton.YesNo) !=
                    AdonisUI.Controls.MessageBoxResult.Yes)
                return;

            await _packagesClient.RemoveFromVersionAsync(Package.Identifier.Id, SelectedVersion);
            LoadVersions();
        }

        private void ButtonSelect_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ParentLayout?.Hide(true);
        }
        #endregion

    }

}
