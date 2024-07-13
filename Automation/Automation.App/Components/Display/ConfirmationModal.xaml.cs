using Automation.App.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
        public IModalContainer? ModalParent { get; set; }
        public string Message { get; set; } = "Are you sure you want to proceed?";

        public ConfirmationModal(string message)
        {
            Message = message;
            InitializeComponent();
        }
    }
}
