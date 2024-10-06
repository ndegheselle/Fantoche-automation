using AdonisUI.Controls;
using Automation.App.Components.Inputs;
using Automation.App.Shared.ApiClients;
using Automation.Shared.Base;
using Automation.Shared.Data;
using Joufflu.Inputs.Components;
using Joufflu.Popups;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.IO;
using System.Windows.Controls;

namespace Automation.App.Views.PackagesPages.Components
{
    /// <summary>
    /// Either create a new package or a package version if an existing package is passed
    /// </summary>
    public class PackageCreateModal : FilePickerModal, IModalContentValidation, INotifyPropertyChanged
    {
        private readonly App _app = App.Current;
        private readonly PackagesClient _packagesClient;
        public event PropertyChangedEventHandler? PropertyChanged;

        public PackageInfos? Package { get; set; }

        public PackageCreateModal(PackageInfos? package = null) : base(
            "New package",
            "Select a .nupkg file",
            new FilePickerOptions() { Filter = "Nuget package (*.nupkg)|*.nupkg", DefaultExtension = "*.nupkg" })
        {
            Package = package;
            _packagesClient = _app.ServiceProvider.GetRequiredService<PackagesClient>();
            Options.ValidButtonText = "Create";
        }

        public async Task<bool> OnValidation()
        {
            SelectedFile.ClearErrors();

            // TODO : show error
            if (string.IsNullOrWhiteSpace(SelectedFile.FilePath) || !File.Exists(SelectedFile.FilePath))
            {
                SelectedFile.AddError("The selected file is invalid.", nameof(FilePickerFile.FilePath));
                return false;
            }

            try
            {
                if (Package != null)
                {
                    Package = await _packagesClient.CreatePackageVersionAsync(Package.Id, SelectedFile.FilePath);
                }
                else
                {
                    Package = await _packagesClient.CreateAsync(SelectedFile.FilePath);
                }
            } catch (ValidationException ex)
            {
                if (ex.Errors != null)
                    SelectedFile.AddErrors(ex.Errors);
                return false;
            }
            return true;
        }
    }

    public class PackageEditModal : PackageEdit, IModalContent
    {
        public ModalOptions? Options { get; } = new ModalOptions();

        public Modal? ParentLayout { get; set; }

        public PackageEditModal(PackageInfos package) : base(package) { Options.Title = package.Id; }
    }

    /// <summary>
    /// Logique d'interaction pour PackageEdit.xaml
    /// </summary>
    public partial class PackageEdit : UserControl, INotifyPropertyChanged
    {
        private IModal _modal => this.GetCurrentModalContainer();
        private readonly App _app = App.Current;
        private readonly PackagesClient _packagesClient;

        public PackageInfos Package { get; set; }

        public PackageEdit(PackageInfos package)
        {
            _packagesClient = _app.ServiceProvider.GetRequiredService<PackagesClient>();
            Package = package;
            InitializeComponent();
        }

        private async void ButtonAdd_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var createPackage = new PackageCreateModal(Package);
            if (await _modal.Show(createPackage) && createPackage.Package != null)
            {
                Package = createPackage.Package;
            }
        }

        private async void MenuItemRemove_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var selectedVersion = ListBoxVersion.SelectedItem as Version;

            if (selectedVersion == null)
                return;

            if (MessageBox.Show(
                    "Are you sure you want to remove this version ?",
                    "Confirmation",
                    AdonisUI.Controls.MessageBoxButton.YesNo) !=
                    AdonisUI.Controls.MessageBoxResult.Yes)
                return;

            var updated = await _packagesClient.RemoveFromVersionAsync(Package.Id, selectedVersion);
            if (updated == null)
                return;

            // TODO : handle last version deletion

            Package = updated;
        }
    }
}
