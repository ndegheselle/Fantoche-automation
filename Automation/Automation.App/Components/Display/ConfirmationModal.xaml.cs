using Automation.App.Base;
using System.Windows.Controls;

namespace Automation.App.Components.Display
{
    /// <summary>
    /// Logique d'interaction pour ConfirmationModal.xaml
    /// </summary>
    public partial class ConfirmationModal : UserControl, IModalContent
    {
        public ModalOptions Options { get; private set; } = new ModalOptions()
        {
            Title = "Confirmation",
            ValidButtonText = "Yes",
            ShowFooter = true
        };
        public IModalContainer? ModalContainer { get; set; }
        public string Message { get; set; } = "Are you sure you want to proceed?";

        public ConfirmationModal(string message)
        {
            Message = message;
            InitializeComponent();
        }
    }
}
