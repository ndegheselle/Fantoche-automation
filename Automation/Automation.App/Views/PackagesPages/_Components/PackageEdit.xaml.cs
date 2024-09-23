using Automation.App.Components.Inputs;
using Automation.App.Shared.ApiClients;
using Automation.Shared;
using Automation.Shared.Base;
using Automation.Shared.Packages;
using Joufflu.Inputs.Components;
using Joufflu.Popups;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Windows.Controls;

namespace Automation.App.Views.PackagesPages.Components
{
    public class PackageCreateModal : FilePickerModal, IModalValidationContent
    {
        private readonly App _app = App.Current;
        private readonly PackagesClient _packagesClient;

        public PackageInfos? Package {  get; set; }

        public PackageCreateModal() : base("Create a new package", "Select a .nupkg file", new FilePickerOptions()
        {
            Filter = "Nuget package (*.nupkg)|*.nupkg",
            DefaultExtension = "*.nupkg"
        })
        {
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
                Package = await _packagesClient.CreateAsync(SelectedFile.FilePath);
            }
            catch (ValidationException ex)
            {
                if (ex.Errors != null)
                    SelectedFile.AddErrors(ex.Errors);
                return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Logique d'interaction pour PackageEdit.xaml
    /// </summary>
    public partial class PackageEdit : UserControl
    {
        public PackageInfos Package { get; set; }
        public PackageEdit(PackageInfos package)
        {
            Package = package;
            InitializeComponent();
        }
    }
}
