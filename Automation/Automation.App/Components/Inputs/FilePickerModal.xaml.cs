using Joufflu.Inputs.Components;
using Joufflu.Popups;
using Joufflu.Shared.Layouts;
using System.Windows.Controls;
using Usuel.Shared;

namespace Automation.App.Components.Inputs
{
    public class FilePickerFile : ErrorValidationModel
    {
        public string FilePath { get; set; } = "";
    }

    /// <summary>
    /// Logique d'interaction pour FilePickerModal.xaml
    /// </summary>
    public partial class FilePickerModal : UserControl, IModalValidationContent
    {
        public ILayout? ParentLayout { get; set; }
        public ModalValidationOptions Options { get; private set; } = new ModalValidationOptions()
        {
            Title = "File select",
            ValidButtonText = "Ok",
        };
        public string SubTitle { get; set; } = string.Empty;

        public FilePickerFile SelectedFile { get; set; } = new FilePickerFile();
        public FilePickerOptions OptionsDialog { get; set; }

        public FilePickerModal(string titre, string subTitle, FilePickerOptions options)
        {
            SubTitle = subTitle;
            OptionsDialog = options;
            Options.Title = titre;
            InitializeComponent();
        }
    }
}
